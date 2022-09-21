/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace onAirXR.Playground.Server {
    #pragma warning disable 0649

    [Serializable]
    public class AirXRPlaygroundConfig {
        public static AirXRPlaygroundConfig config { get; private set; }

        public static void LoadOnce(AirXRPlayground.Mode modeInEditor, AirXRPlaygroundConfigMulticast multicastInEditor) {
            if (config != null) { return; }

            if (Application.isEditor) {
                config = new AirXRPlaygroundConfig {
                    mode = modeInEditor == AirXRPlayground.Mode.Observer ? "observer" : "player",
                    multicast = multicastInEditor,
                    displays = AirXRPlaygroundConfigDisplays.Default
                };
            }
            else {
                config = new AirXRPlaygroundConfig();
                config.ParseCommandLine();
            }
        }

        public string mode;
        public AirXRPlaygroundConfigMulticast multicast;
        public AirXRPlaygroundConfigDisplays displays;
        public AirXRPlaygroundConfigExtension[] extensions;

        public bool isValid => string.IsNullOrEmpty(mode) == false;

        public void ParseCommandLine() {
            var args = AXRUtils.ParseCommandLine(Environment.GetCommandLineArgs());
            if (args == null || args.Count == 0) { return; }

            if (args.ContainsKey("config")) {
                try {
                    var reader = JsonUtility.FromJson<Reader>(File.ReadAllText(args["config"]));

                    mode = reader.config.mode;
                    multicast = reader.config.multicast;
                    displays = reader.config.displays;
                    extensions = reader.config.extensions;
                }
                catch (Exception e) {
                    Debug.LogErrorFormat("[ERROR] failed to load config: {0}", e.ToString());
                }
            }
        }

        public AirXRPlayground.Mode GetMode() {
            switch (mode) {
                case "observer":
                    return AirXRPlayground.Mode.Observer;
                default:
                    return AirXRPlayground.Mode.Player;
            }
        }

        [Serializable]
        private struct Reader {
            [SerializeField] private AirXRPlaygroundConfig playground;

            // for backward compatibility
            [SerializeField] private AirXRPlaygroundConfig circlevr;

            public AirXRPlaygroundConfig config => playground.isValid ? playground : circlevr;
        }
    }

    [Serializable]
    public struct AirXRPlaygroundConfigMulticast {
        public string address;
        public int port;
        public string hint;

        public bool isValid => string.IsNullOrEmpty(address) == false &&
                               0 < port && port <= 65535;
    }

    [Serializable]
    public struct AirXRPlaygroundConfigExtension {
        public string name;
        public string address;
    }

    [Serializable]
    public struct AirXRPlaygroundConfigDisplays {
        public static AirXRPlaygroundConfigDisplays Default = new AirXRPlaygroundConfigDisplays {
            layout = new string[] { "oooo oooo oooo 0123" }
        };

        private const float DefaultFOV = 70.0f;

        public const int InvalidDisplayIndex = -1;

        [Serializable]
        public struct ViewDesc {
            public string id;
            public float fov;
            public float aspect;
            public int depth;
        }

        public struct View {
            public char id;
            public float fov;
            public float aspect;
            public int depth;
            public Rect viewRect;
            public int targetDisplayIndex;

            public override string ToString() {
                return string.Format("{0}: fov {1}, aspect {2}, depth {3}, view rect {4}, display {5}",
                                     id, fov, aspect, depth, viewRect, targetDisplayIndex);
            }
        }

        public string[] layout;
        public ViewDesc[] views;
        public string mode;
        public float fov;
        public int depth;

        public bool isValid => layout != null;

        public void Load(string json) {
            layout = null;
            views = null;

            JsonUtility.FromJsonOverwrite(json, this);

            if (layout == null || layout.Length == 0) {
                layout = Default.layout;
            }
        }

        public List<View> GetViews() {
            var descs = new Dictionary<char, ViewDesc>();
            if (views != null) {
                foreach (var view in views) {
                    var seq = view.id.ToCharArray();
                    if (seq.Length == 0) { continue; }

                    descs[seq[0]] = view;
                }
            }
            var result = new List<View>();
            for (var displayIndex = 0; displayIndex < layout.Length; displayIndex++) {
                getViewsOfDisplay(descs, layout[displayIndex], layout.Length > 1 ? displayIndex : InvalidDisplayIndex, result);
            }
            return result;
        }

        private void getViewsOfDisplay(Dictionary<char, ViewDesc> desc, string display, int targetDisplayIndex, List<View> result) {
            if (display == null ||
                (targetDisplayIndex != InvalidDisplayIndex && targetDisplayIndex >= Display.displays.Length)) { return; }

            var rows = Regex.Split(display, @"[\s,;]+");
            var rects = new List<ViewRect>();

            var width = -1;
            for (var row = 0; row < rows.Length; row++) {
                if (string.IsNullOrEmpty(rows[row])) { continue; }

                if (width == -1) {
                    width = rows[row].Length;
                }
                else if (width != rows[row].Length) {
                    Debug.LogWarning("[WARNING] invalid display layout of index " + targetDisplayIndex);
                    return;
                }

                var seq = rows[row].ToCharArray();
                var id = '-';
                var start = 0;
                for (var scan = 0; scan <= seq.Length; scan++) {
                    if (scan == 0) {
                        id = seq[scan];
                    }
                    else if (scan == seq.Length || seq[scan] != id) {
                        var extended = false;
                        foreach (var rect in rects) {
                            if (rect.id == id && rect.rect.yMax == row && (scan - start) == rect.rect.width) {
                                rect.rect = new RectInt(rect.rect.xMin, rect.rect.yMin, rect.rect.width, rect.rect.height + 1);
                                extended = true;
                                break;
                            }
                        }

                        if (extended == false) {
                            rects.Add(new ViewRect {
                                id = id,
                                rect = new RectInt(start, row, scan - start, 1)
                            });
                        }

                        if (scan < seq.Length) {
                            id = seq[scan];
                        }
                        start = scan;
                    }
                }
            }

            if (width <= 0) { return; }
            var height = rows.Length;
            var layoutAspect = (float)width / height;
            var displayAspect = targetDisplayIndex != InvalidDisplayIndex ? (float)Display.displays[targetDisplayIndex].renderingWidth / Display.displays[targetDisplayIndex].renderingHeight :
                                                                            (float)Screen.width / Screen.height;

            foreach (var rect in rects) {
                var fieldOfView = desc.ContainsKey(rect.id) && desc[rect.id].fov > 0 ? desc[rect.id].fov : fov;
                var displayDepth = desc.ContainsKey(rect.id) && desc[rect.id].depth != 0 ? desc[rect.id].depth : depth;
                var viewWidth = (float)rect.rect.width / width;
                var viewHeight = (float)rect.rect.height / height;

                var rectAspect = (float)rect.rect.width / rect.rect.height;
                var aspect = desc.ContainsKey(rect.id) && desc[rect.id].aspect > 0 ? desc[rect.id].aspect : displayAspect / layoutAspect * rectAspect;

                result.Add(new View {
                    id = rect.id,
                    fov = fieldOfView > 0 ? fieldOfView : (rect.id == 'o' ? 0 : DefaultFOV),
                    aspect = aspect,
                    depth = displayDepth,
                    viewRect = new Rect((float)rect.rect.xMin / width, (1 - (float)rect.rect.yMin / height) - viewHeight, viewWidth, viewHeight),
                    targetDisplayIndex = targetDisplayIndex
                });
            }
        }

        private class ViewRect {
            public char id;
            public RectInt rect;
        }
    }

    #pragma warning restore 0649
}