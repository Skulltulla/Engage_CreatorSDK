﻿using UnityEngine;
using UnityEditor;
using System.Net;
using System.IO;
using System;
using System.Xml;
using System.Security.Cryptography;

namespace AssetBundles
{
    [InitializeOnLoad]
    public class UpdateManager : EditorWindow
    {
        static bool checkComplete = false;
        static bool updateComplete = false;
        static bool updateInProgress = false;
        static bool automaticUpdatesEnabled = false;
        static bool packageUpToDate = false;

        float defaultLabelWidth;
        readonly float guiLabelWidth = 160f;
        static readonly string _filepath = "CreatorSDK.unitypackage";
        //Master URL
        //readonly string _packageUrl = "https://github.com/immersivevreducation/Engage_CreatorSDK/blob/master/CreatorSDK.unitypackage?raw=true";
        static readonly string _packageUrl = "https://github.com/james-ivre/Test_Repo/blob/master/CreatorSDK.unitypackage?raw=true";
        static readonly string _localManifestPath = "manifest.xml";

        [MenuItem("Creator SDK/Check for updates")]
        public static void ShowUpdateWindow()
        {
            GetWindow<UpdateManager>(false, "Update manager", true);
        }

        static UpdateManager()
        {
            automaticUpdatesEnabled = bool.Parse(GetValueFromXML(File.ReadAllText(_localManifestPath), "packageData/autoupdate"));
            if (automaticUpdatesEnabled)
                ImportPackage();
        }

        private void OnGUI()
        {
            GUILayout.Label("Creator SDK package may not be up to date with latest version.");
            EditorGUILayout.Space();
            if (GUILayout.Button("Check for updates") && !updateInProgress)
            {
                updateInProgress = true;
                checkComplete = true;
                ImportPackage();
            }
            EditorGUILayout.Space();

            defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = guiLabelWidth;

            if (automaticUpdatesEnabled = EditorGUILayout.Toggle("Enabled automatic updates", automaticUpdatesEnabled))
            {
                WriteDataToXML(File.ReadAllText(_localManifestPath), "packageData/autoupdate", automaticUpdatesEnabled);
            }

            EditorGUIUtility.labelWidth = defaultLabelWidth;

            if (checkComplete)
            {
                if (updateComplete)
                {
                    if (automaticUpdatesEnabled)
                    {
                        EditorUtility.DisplayDialog("CreatorSDK Updater", "CreatorSDK updated to latest version!", "OK");
                        updateComplete = false;
                    }
                    GUILayout.Label("Creator SDK updated to latest version!");
                }
                else if (packageUpToDate)
                {
                    if (automaticUpdatesEnabled)
                    {
                        EditorUtility.DisplayDialog("CreatorSDK Updater", "CreatorSDK already up to date!", "OK");
                        updateComplete = false;
                    }
                    GUILayout.Label("Creator SDK is already up to date with latest version!");
                }
                else
                {
                    GUILayout.Label("Downloading package from server, this may take several moments...");
                }
            }
        }

        private static void ImportPackage()
        {
            WebClient wc = new WebClient();
            Uri _uri = new Uri(_packageUrl);
            wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
            try
            {
                wc.DownloadFileAsync(_uri, "CreatorSDK");
            }
            catch
            {
                throw new FileNotFoundException();
            }
        }

        private static bool PackageIsUpToDate(string _path)
        {
            return GetMD5Checksum(_path) == GetValueFromXML(File.ReadAllText(_localManifestPath), "packageData/checksum");
        }

        private static void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            updateInProgress = false;
            try
            {
                if (File.Exists(_filepath))
                    FileUtil.DeleteFileOrDirectory(_filepath);
                FileUtil.MoveFileOrDirectory("CreatorSDK", _filepath);

                if (File.Exists(_filepath))
                {
                    if (PackageIsUpToDate(_filepath))
                    {
                        Debug.Log("Already up to date!");
                        packageUpToDate = true;
                        return;
                    }
                    else
                    {
                        Debug.Log("Importing updated package");
                        AssetDatabase.ImportPackage(_filepath, false);
                        updateComplete = true;
                        WriteDataToXML(File.ReadAllText(_localManifestPath), "packageData/checksum", GetMD5Checksum(_filepath));
                    }
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            catch
            {
                throw e.Error;
            }
        }

        private static string GetValueFromXML(string _xml, string _xpath)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(_xml);
            string xpath = _xpath;
            var node = xDoc.SelectSingleNode(xpath);

            return node.InnerXml;
        }

        private static void WriteDataToXML(string _xml, string _xpath, string _value)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(_xml);
            string xpath = _xpath;
            var node = xDoc.SelectSingleNode(xpath);
            node.InnerXml = _value;
            xDoc.Save(_localManifestPath);
        }

        private static void WriteDataToXML(string _xml, string _xpath, int _value)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(_xml);
            string xpath = _xpath;
            var node = xDoc.SelectSingleNode(xpath);
            node.InnerXml = _value.ToString();
            xDoc.Save(_localManifestPath);
        }

        private static void WriteDataToXML(string _xml, string _xpath, bool _value)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(_xml);
            string xpath = _xpath;
            var node = xDoc.SelectSingleNode(xpath);
            node.InnerXml = _value.ToString();
            xDoc.Save(_localManifestPath);
        }

        private static string GetMD5Checksum(string _path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(_path))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}