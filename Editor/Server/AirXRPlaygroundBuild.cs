/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace onAirXR.Playground.Server.Editor {
    public class AirXRPlaygroundBuild {
        private class BuilderWindow : EditorWindow {
            private const float LabelWidth = 180;

            private ContentDescription _contentDesc;

            [MenuItem("onAirXR/Playground/Build...", false, 0)]
            public static void ShowWindow() {
                GetWindow<BuilderWindow>("onAirXR Playground Build");
            }

            private void OnEnable() {
                if (_contentDesc == null) {
                    _contentDesc = new ContentDescription();
                    _contentDesc.Reload();
                }
            }

            private void OnGUI() {
                bool shouldUpdateContentDesc = false;

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUI.indentLevel++;

                renderSection(Styles.labelBuild, () => {
                    renderTextField(Styles.labelCompanyName, PlayerSettings.companyName, value => {
                        PlayerSettings.companyName = value;
                    });
                    renderTextField(Styles.labelContentName, PlayerSettings.productName, value => {
                        PlayerSettings.productName = value;
                        _contentDesc.launch.path = value + ".exe";
                        shouldUpdateContentDesc = true;
                    });
                });

                renderSection(Styles.labelContentDescription, () => {
                    renderTextField(Styles.labelTitle, _contentDesc.description.title, value => {
                        _contentDesc.description.title = value;
                        shouldUpdateContentDesc = true;
                    });
                    renderTextField(Styles.labelVersion, _contentDesc.description.version, value => {
                        _contentDesc.description.version = value;
                        shouldUpdateContentDesc = true;
                    });
                    renderTextArea(Styles.labelDescription, _contentDesc.description.description, value => {
                        _contentDesc.description.description = value;
                        shouldUpdateContentDesc = true;
                    });

                    var thumbnail = _contentDesc.description.thumbnails.Length > 0 ? _contentDesc.description.thumbnails[0] : "";
                    renderTextField(Styles.labelThumbnail, thumbnail, value => {
                        _contentDesc.description.thumbnails = string.IsNullOrEmpty(value) == false ? new string[] { value } : new string[] { };
                        shouldUpdateContentDesc = true;
                    });

                    renderTextField(Styles.labelPlayers, _contentDesc.group.instanceCount.ToString(), value => {
                        _contentDesc.group.instanceCount = int.TryParse(value, out int parsed) ? parsed : 0;
                        shouldUpdateContentDesc = true;
                    });

                    renderCommandsMaskField(Styles.labelCommands, _contentDesc.group.commands, value => {
                        _contentDesc.group.commands = value;
                        shouldUpdateContentDesc = true;
                    });
                });

                var shouldBuild = GUILayout.Button(Styles.buttonBuild, Styles.styleButtonBuild);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                if (shouldUpdateContentDesc || shouldBuild) {
                    _contentDesc.Save();
                }

                if (shouldBuild) {
                    buildContent(false);
                }
            }

            private void renderSection(GUIContent label, Action render) {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUILayout.Space();

                render();

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            private string renderTextField(GUIContent label, string value, Action<string> onChange = null) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(LabelWidth));
                var next = EditorGUILayout.TextField(value);
                EditorGUILayout.EndHorizontal();

                if (next != value) {
                    onChange?.Invoke(next);
                }
                return next;
            }

            private string renderTextArea(GUIContent label, string value, Action<string> onChange = null) {
                var style = EditorStyles.textField;
                style.wordWrap = true;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(LabelWidth));
                var next = EditorGUILayout.TextField(value, style, GUILayout.Height(52));
                EditorGUILayout.EndHorizontal();

                if (next != value) {
                    onChange?.Invoke(next);
                }
                return next;
            }

            private string[] renderCommandsMaskField(GUIContent label, string[] commands, Action<string[]> onChange = null) {
                var mask = 0;
                foreach (var command in commands) {
                    switch (command) {
                        case "play":
                            mask |= (int)Command.Play;
                            break;
                        case "pause":
                            mask |= (int)Command.Pause;
                            break;
                        case "stop":
                            mask |= (int)Command.Stop;
                            break;
                        case "next":
                            mask |= (int)Command.Next;
                            break;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(LabelWidth));

                EditorGUILayout.BeginVertical();
                var nextMask = EditorGUILayout.MaskField(mask, _allCommands);
                var nextCommands = new List<string>();
                if ((nextMask & (int)Command.Play) != 0) {
                    nextCommands.Add("play");
                }
                if ((nextMask & (int)Command.Pause) != 0) {
                    nextCommands.Add("pause");
                }
                if ((nextMask & (int)Command.Stop) != 0) {
                    nextCommands.Add("stop");
                }
                if ((nextMask & (int)Command.Next) != 0) {
                    nextCommands.Add("next");
                }
                EditorGUILayout.LabelField(string.Format("[{0}]", string.Join(", ", nextCommands)), Styles.styleNote);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

                if (nextCommands.Count != commands.Length) {
                    var next = nextCommands.ToArray();
                    onChange?.Invoke(next);
                    return next;
                }
                else {
                    return commands;
                }
            }

            private void buildContent(bool archiveOnly) {
                try {
                    var tempPath = Path.Combine(Directory.GetParent(Application.temporaryCachePath).Parent.FullName, "onairxr-playground");
                    if (Directory.Exists(tempPath) == false) {
                        Directory.CreateDirectory(tempPath);
                    }

                    var buildPath = Path.Combine(tempPath, PlayerSettings.productName);
                    if (archiveOnly) {
                        if (Directory.Exists(buildPath) == false) {
                            EditorUtility.DisplayDialog("Build Cancelled", "Does not exist the last build to archive.", "Close");
                            return;
                        }
                    }
                    else if (Directory.Exists(buildPath)) {
                        Directory.Delete(buildPath, true);
                    }

                    var archiveName = string.Format("{0}_{1}", PlayerSettings.productName, _contentDesc.description.version);
                    var archivePath = EditorUtility.SaveFilePanel("Save the Build To...", "", archiveName, "zip");
                    if (string.IsNullOrEmpty(archivePath)) { return; }

                    Debug.LogFormat("{0} -> {1}", buildPath, archivePath);

                    if (archiveOnly == false) {
                        Directory.CreateDirectory(buildPath);

                        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,
                                                               Path.Combine(buildPath, PlayerSettings.productName + ".exe"),
                                                               BuildTarget.StandaloneWindows64,
                                                               BuildOptions.None);
                        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) {
                            throw new UnityException(report.summary.result.ToString());
                        }
                    }

                    if (File.Exists(archivePath)) {
                        File.Delete(archivePath);
                    }

                    var buildContentDescFolder = Path.Combine(buildPath, ".onairvr");
                    if (Directory.Exists(buildContentDescFolder)) {
                        Directory.Delete(buildContentDescFolder, true);
                    }
                    Directory.CreateDirectory(buildContentDescFolder);

                    var contentDescFolder = new DirectoryInfo(ContentDescription.contentDescFolder);
                    var contentDescFiles = contentDescFolder.GetFiles();
                    foreach (var file in contentDescFiles) {
                        file.CopyTo(Path.Combine(buildContentDescFolder, file.Name), false);
                    }

                    ZipFile.CreateFromDirectory(buildPath, archivePath, System.IO.Compression.CompressionLevel.Optimal, true, System.Text.Encoding.UTF8);

                    System.Diagnostics.Process.Start(Directory.GetParent(archivePath).FullName);
                    Debug.Log("[onAirXR Playground Build] successfully built content: " + archivePath);
                }
                catch (Exception e) {
                    Debug.LogErrorFormat("[onAirXR Playground Build] failed to build content: {0}", e.ToString());
                }
            }

            private enum Command : int {
                Play = 0x01,
                Pause = 0x02,
                Stop = 0x04,
                Next = 0x08
            }

            private string[] _allCommands = new string[] {
                "Play",
                "Pause",
                "Stop",
                "Next"
            };
        }

        private class Styles {
            public static GUIContent labelBuild = new GUIContent("Build");
            public static GUIContent labelCompanyName = new GUIContent("Company Name");
            public static GUIContent labelContentName = new GUIContent("Content Name");

            public static GUIContent labelContentDescription = new GUIContent("Content Description");
            public static GUIContent labelTitle = new GUIContent("Title");
            public static GUIContent labelVersion = new GUIContent("Version");
            public static GUIContent labelDescription = new GUIContent("Description");
            public static GUIContent labelThumbnail = new GUIContent("Thumbnail");
            public static GUIContent labelPlayers = new GUIContent("Players");
            public static GUIContent labelCommands = new GUIContent("Supported Commands");

            public static GUIContent buttonBuild = new GUIContent("Build...");
            public static GUIContent buttonArchive = new GUIContent("Archive Last Build...");

            public static GUIStyle styleNote = new GUIStyle(EditorStyles.wordWrappedLabel);
            public static GUIStyle styleButtonBuild = new GUIStyle(EditorStyles.miniButton);

            static Styles() {
                styleNote.fontStyle = FontStyle.Italic;
                styleButtonBuild.fixedHeight = 24;
                styleButtonBuild.margin = new RectOffset {
                    left = 20,
                    top = styleButtonBuild.margin.top * 2,
                    right = styleButtonBuild.margin.right,
                    bottom = styleButtonBuild.margin.bottom * 2
                };
            }
        }

        [Serializable]
        private class ContentDescription {
            public static string contentDescFolder => Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".onairvr");
            public static string contentDescFile => Path.Combine(contentDescFolder, "content.json");

            public Description description;
            public Launch launch;
            public Group group;

            public ContentDescription() {
                description = new Description {
                    title = "Unknown",
                    version = "0",
                    description = "",
                    thumbnails = new string[] { },
                    screenshots = new string[] { }
                };
                launch = new Launch {
                    path = ""
                };
                group = new Group {
                    commands = new string[] { "play", "stop", "pause", "next" }
                };
            }

            public void Reload() {
                try {
                    if (Directory.Exists(contentDescFolder) == false) {
                        Directory.CreateDirectory(contentDescFolder);
                    }
                    if (File.Exists(contentDescFile) == false) {
                        // calling PlayerSettings.productName in constructor yields an error (not allowed during serialization)
                        launch.path = string.Format("{0}.exe", PlayerSettings.productName);

                        Save();
                        return;
                    }

                    JsonUtility.FromJsonOverwrite(File.ReadAllText(contentDescFile), this);
                }
                catch (Exception e) {
                    Debug.LogErrorFormat("[onAirXR Playground Build] failed to load content description: {0}", e.ToString());
                }
            }

            public void Save() {
                try {
                    var json = JsonUtility.ToJson(this, true);

                    using (var writer = File.CreateText(contentDescFile)) {
                        writer.Write(json);
                    }
                }
                catch (Exception e) {
                    Debug.LogErrorFormat("[onAirXR Playground Build] failed to save content description: {0}", e.ToString());
                }
            }

            [Serializable]
            public struct Description {
                public string title;
                public string version;
                public string description;
                public string[] thumbnails;
                public string[] screenshots;
            }

            [Serializable]
            public struct Launch {
                public string path;
            }

            [Serializable]
            public struct Group {
                public int instanceCount;
                public string[] commands;
            }
        }
    }
}
