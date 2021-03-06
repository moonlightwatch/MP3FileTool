﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Drawing;
using System.Threading.Tasks;

namespace 音频文件整理工具
{
    internal class MP3FileTool
    {
        public event MP3LoadOneEventHandler OnLoadOne = new MP3LoadOneEventHandler((object sender, MP3LoadOneEventArgs e) => { });

        public event EventHandler LoadCompleted = new EventHandler((object sender, EventArgs e) => { });

        public event MP3FindOneEventHandler OnFindOne = new MP3FindOneEventHandler((object sender, MP3FindOneEventArgs e) => { });

        /// <summary>
        /// MP3文件信息集合
        /// </summary>
        private List<MP3FileInfo> fileInfos = new List<MP3FileInfo>();

        internal async System.Threading.Tasks.Task LoadFromFolderAsync(string path, bool reopen)
        {
            if (!Directory.Exists(path))
            {
                return;
            }
            if (reopen == true)
            {
                fileInfos.Clear();
            }
            MP3Loader loader = new MP3Loader();
            var files = Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories);
            foreach (var item in files)
            {
                var tmp = await loader.LoadFromFile(item);
                if (tmp != null)
                {
                    OnLoadOne(this, new MP3LoadOneEventArgs()
                    {
                        Itme = tmp,
                        Index = fileInfos.Count + 1,
                        Total = files.Length
                    });
                    fileInfos.Add(tmp);
                }
            }
            LoadCompleted(this, new EventArgs());
        }

        internal async System.Threading.Tasks.Task LoadFromFileAsync(string path, bool reopen)
        {
            if (!File.Exists(path))
            {
                return;
            }
            if (reopen == true)
            {
                fileInfos.Clear();
            }
            MP3Loader loader = new MP3Loader();
            var tmp = await loader.LoadFromFile(path);
            if (tmp != null)
            {
                OnLoadOne(this, new MP3LoadOneEventArgs()
                {
                    Itme = tmp,
                    Index = 1,
                    Total = 1
                });
                fileInfos.Add(tmp);
            }
            LoadCompleted(this, new EventArgs());
        }

        internal MP3FileInfo[] GetAllFileInfos()
        {
            return fileInfos.ToArray();
        }

        internal MP3FileInfo[] RenameAllFile(MP3FileInfo[] files, RenameFormat format)
        {
            foreach (var item in files)
            {
                item.FileName = item.FormatFileName(format);
            }
            return files;
        }

        internal MP3FileInfo[] GetFileByAlbum(string album)
        {
            return fileInfos.FindAll(f => f.Album == album).ToArray();
        }

        internal MP3FileInfo[] GetFileByPerformer(string performer)
        {
            return fileInfos.FindAll(f => f.Performer == performer).ToArray();
        }

        internal MP3FolderInfo[] FolderByAlbum(MP3FileInfo[] files)
        {
            List<MP3FolderInfo> result = new List<MP3FolderInfo>();
            foreach (var item in files)
            {
                var tmp = result.Find(f => f.Name == item.Album);
                if (tmp == null)
                {
                    MP3FolderInfo newFolder = new MP3FolderInfo();
                    newFolder.Name = item.Album;
                    newFolder.FileInfos.Add(item);
                    result.Add(newFolder);
                }
                else
                {
                    tmp.FileInfos.Add(item);
                }
            }
            return result.ToArray();
        }

        internal MP3FolderInfo[] FolderByPerformer(MP3FileInfo[] files)
        {
            List<MP3FolderInfo> result = new List<MP3FolderInfo>();
            foreach (var item in files)
            {
                var tmp = result.Find(f => f.Name == item.Performer);
                if (tmp == null)
                {
                    MP3FolderInfo newFolder = new MP3FolderInfo();
                    newFolder.Name = item.Performer;
                    newFolder.FileInfos.Add(item);
                    result.Add(newFolder);
                }
                else
                {
                    tmp.FileInfos.Add(item);
                }
            }
            return result.ToArray();
        }

        internal bool SaveToFolder(string targetPath, MP3FileInfo[] files)
        {
            if (!Directory.Exists(targetPath))
            {
                return false;
            }

            foreach (var item in files)
            {
                try
                {
                    File.Copy(item.FilePath, Path.Combine(targetPath, item.FileName), true);
                }
                catch (Exception exc)
                {
                    System.Windows.Forms.MessageBox.Show(exc.Message, "保存出错");
                    return false;
                }
            }
            return true;
        }

        internal bool SaveToFolder(string targetPath, MP3FolderInfo[] folders)
        {
            if (!Directory.Exists(targetPath))
            {
                return false;
            }
            foreach (var folder in folders)
            {
                var files = folder.FileInfos;
                if (files.Count > 0)
                {
                    Directory.CreateDirectory(Path.Combine(targetPath, folder.Name));
                }
                foreach (var item in files)
                {
                    try
                    {
                        File.Copy(item.FilePath, Path.Combine(targetPath, folder.Name, item.FileName), true);
                    }
                    catch (Exception exc)
                    {
                        System.Windows.Forms.MessageBox.Show(exc.Message, "保存出错");
                        return false;
                    }
                }
            }
            return true;
        }

        internal async Task<MP3FileInfo[]> Search(string searchText, SearchType searchType)
        {
            Task<MP3FileInfo[]> task = new Task<MP3FileInfo[]>((object obj) =>
            {
                var text = (obj as object[])[0].ToString();
                var type = (SearchType)Enum.Parse(typeof(SearchType), (obj as object[])[1].ToString());
                MP3FileInfo[] infos = (obj as object[])[2] as MP3FileInfo[];
                if (string.IsNullOrEmpty(text))
                {
                    int n = 1;
                    foreach (MP3FileInfo item in infos)
                    {
                        OnFindOne(this, new MP3FindOneEventArgs()
                        {
                            Itme = item,
                            Count = n
                        });
                        n += 1;
                    }
                    return infos;
                }
                List<MP3FileInfo> result = new List<MP3FileInfo>();
                foreach (MP3FileInfo item in infos)
                {
                    switch (type)
                    {
                        case SearchType.Album:
                            if (item.Album.Contains(text))
                            {
                                result.Add(item);
                                OnFindOne(this, new MP3FindOneEventArgs()
                                {
                                    Itme = item,
                                    Count = result.Count
                                });
                            }
                            break;
                        case SearchType.Performer:
                            if (item.Performer.Contains(text))
                            {
                                result.Add(item);
                                OnFindOne(this, new MP3FindOneEventArgs()
                                {
                                    Itme = item,
                                    Count = result.Count
                                });
                            }
                            break;
                        case SearchType.Title:
                            if (item.Title.Contains(text))
                            {
                                result.Add(item);
                                OnFindOne(this, new MP3FindOneEventArgs()
                                {
                                    Itme = item,
                                    Count = result.Count
                                });
                            }
                            break;
                    }
                }
                return result.ToArray();
            }, new object[] { searchText, searchType, this.fileInfos.ToArray() });
            task.Start();
            return await task;
        }
    }

    internal enum SearchType
    {
        Title,
        Performer,
        Album
    }
}
