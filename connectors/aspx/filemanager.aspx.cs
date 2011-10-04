using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.IO;

/* Connector for aspx
 * LDTECH ,.JSC 2010
 * http://www.ldtech.com.vn
 * Written by Toan Nguyen 
 * Changed / Updated by: EC
 * 
 * Source: http://forum.filemanager.corefive.com/#Topic/18308000000094020
 * Changes:
 * - Added Thumbnailing
 * - Added Bugfix: No refresh after upload (http://forum.filemanager.corefive.com/topic/a-connector-for-asp-net#18308000000362028)
 * - Added Quick Upload feature
 *      Connector settings FCK editor.config.js:
 *      FCKConfig.LinkUploadURL = FCKConfig.BasePath + "c5filemanager/connectors/aspx/filemanager.aspx?mode=QuickUpload&version=fck";
 *      Connector settings CK editor.config.js (not tested):
 *      <CKSomethings.BasePath> + "c5filemanager/connectors/aspx/filemanager.aspx?mode=QuickUpload&version=ck";
 * - Added Switch for file replace / rename the old file upload new one
 * - Added image crop (button logic copied from http://forum.filemanager.corefive.com/topic/c5-filemanager-resize-upload-image#18308000000332003)
 * 
 */

namespace C5FileManager.connectors.aspx
{
    public partial class Filemanager : System.Web.UI.Page
    {
        #region Disable ASP.NET features

        /// <summary>
        /// Theming is disabled as it interferes in the connector response data.
        /// </summary>
        public override bool EnableTheming
        {
            get { return false; }
            set { /* Ignore it with no error */ }
        }

        /// <summary>
        /// Master Page is disabled as it interferes in the connector response data.
        /// </summary>
        public override string MasterPageFile
        {
            get { return null; }
            set { /* Ignore it with no error */ }
        }

        /// <summary>
        /// Theming is disabled as it interferes in the connector response data.
        /// </summary>
        public override string Theme
        {
            get { return ""; }
            set { /* Ignore it with no error */ }
        }

        /// <summary>
        /// Theming is disabled as it interferes in the connector response data.
        /// </summary>
        public override string StyleSheetTheme
        {
            get { return ""; }
            set { /* Ignore it with no error */ }
        }

        #endregion

        #region properties
        public string BaseUrl = "";
        public string BaseFilemanagerLocation = "";
        public string BaseQuickUploadLocation = "";
        public string ExistingFileOption = "";
        #endregion

        /// <summary>
        /// Page load
        /// </summary>
        public void Page_Load()
        {
            // sorry, you're not allowed to be here
            if (!CheckAuthentication())
            {
                Response.Write(CreateError("No Permission"));
                return;
            }

            // Config settings
            SetConfig();

            string mode;
            string response = "";

            Response.ContentType = "text/plain";

            if (Request.QueryString["mode"] != null)
            {
                mode = Request.QueryString["mode"];
                switch (mode.ToLowerInvariant())
                {
                    case "getinfo"          : response = GetInfo();     break;
                    case "getfolder"        : response = GetFolder();   break;
                    case "rename"           : response = Rename();      break;
                    case "delete"           : response = Delete();      break;
                    case "addfolder"        : response = AddFolder();   break;
                    case "quickupload"      : response = QuickUpload(); break;
                    case "crop"             : response = Crop();        break;
                    case "resize"           : response = Resize();      break;
                    case "download"         : Download();               break;
                    default                 : CreateError("No Mode");   break;
                }
            }
            else if (Request.Form["mode"] != null)
            {
                mode = Request.Form["mode"];

                switch (mode)
                {
                    case "add":
                        response = Add();
                        Response.ContentType = "text/html";
                        break;

                    default:
                        CreateError("No Mode");
                        break;
                }
            }

            if (response.Length > 0)
            {
                Response.Write(response);
            }
        }

        /// <summary>
        /// Check authentication
        /// </summary>
        /// <returns></returns>
        public bool CheckAuthentication()
        {
            // ORIGINAL: return (Session["SUCCESSFULLOGINED"] != null && Session["SUCCESSFULLOGINED"].ToString() == "Yes"); //return false;
            return true;
        }

        public void SetConfig()
        {
            // The base URL used to reach files in CKFinder through the browser.  
            // Be shure there is no '/' at the end
            // IN ORIGINAL VERSION: BaseUrl = "/userfiles"; 
            BaseUrl = "";

            // The installation location that we place FileManager files
            // ORIGINIAL name and location: BaseInstall = "/tools/FileManager/";
            BaseFilemanagerLocation = "/";

            // Location for quick upload
            // That is the fileRoot from the filemanger.config.js + 'quickupload directory'
            BaseQuickUploadLocation = "/userfiles/quickupload/";

            // What do do when the file exists on disk? 
            // Options are: rename / replace
            ExistingFileOption = "replace";
        }

        private string GetInfo(string path, string fullPhysicalPath)
        {
            if (Request.QueryString["path"] == null)
            {
                return "";
            }

            string fileName = "";
            string fileType = "";
            string preview = "";
            string dateCreated = "";
            string dateModified = "";
            string height = "";
            string width = "";
            string size = "";
            string error = "";
            string code = "0";

            if (File.Exists(fullPhysicalPath))
            {
                // file
                FileInfo fi = new FileInfo(fullPhysicalPath);
                fileName = fi.Name;
                fileType = fi.Extension.Replace(".", "");

                if (fileType.Length == 0)
                {
                    fileType = "txt";
                }
                
                if (IsImageExtension(fileType))
                {
                    // document is image

                    // ORIGINAL: preview = path;
                    if (Request.QueryString["showThumbs"] == "true")
                    {
                        // document overview
                        // show thumbnails (or generate them first)
                        preview = GetPreviewImage(path, fullPhysicalPath);
                        
                        int tempHeight = 0;
                        int tempWidth = 0;

                        GetImageDimensions(GetPathWithoutFilename(fullPhysicalPath, GetShortFileName(fullPhysicalPath)) + GetThumbNameFromFullPhysicalPath(fullPhysicalPath), out tempHeight, out tempWidth);
                        height = tempHeight.ToString();
                        width = tempWidth.ToString();
                    }else
                    {
                        // document detail
                        preview = BaseUrl + path;
                    }
                }
                else
                {
                    // document is no image
                    preview = BaseFilemanagerLocation + "images/fileicons/" + GetFileTypeIconName(fileType) + ".png";
                }

                dateCreated = fi.CreationTime.ToString();
                dateModified = fi.LastWriteTime.ToString();
                size = fi.Length.ToString();

                if (Request.QueryString["getsize"] != null && Request.QueryString["getsize"] == "true")
                {
                    int tempHeight = 0;
                    int tempWidth = 0;

                    GetImageDimensions(fullPhysicalPath, out tempHeight, out tempWidth);
                    height = tempHeight.ToString();
                    width = tempWidth.ToString();
                }
            }
            else if (Directory.Exists(fullPhysicalPath))
            {
                // directory
                DirectoryInfo di = new DirectoryInfo(fullPhysicalPath);
                fileName = di.Name;
                fileType = "dir";
                preview = BaseFilemanagerLocation + "images/fileicons/_Close.png";
                dateCreated = di.CreationTime.ToString();
                dateModified = di.LastWriteTime.ToString();
            }
            else
            {
                // not a file or directory
                error = "no file or directory";
                code = "-1";
            }

            string retVal = "{ \"Path\":" + EnquoteJson(path) + ",\r\n" +
                " \"Filename\":" + EnquoteJson(fileName) + ",\r\n" +
                " \"File Type\":" + EnquoteJson(fileType) + ",\r\n" +
                " \"Preview\":" + EnquoteJson(preview) + ",\r\n" +
                " \"Properties\":{\r\n" +
                "       \"Date Created\":" + EnquoteJson(dateCreated) + ",\r\n" +
                "       \"Date Modified\":" + EnquoteJson(dateModified) + "\r\n";
                if (height.Length > 0)
                {
                    retVal += "       ,\"Height\":" + height + "\r\n";
                }

                if (width.Length > 0)
                {
                    retVal += "       ,\"Width\":" + width + "\r\n";
                }

                if (size.Length > 0)
                {
                    retVal += "       ,\"Size\":" + size + "\r\n";
                }

            retVal += "},\r\n" +
                " \"Error\":" + EnquoteJson(error) + ",\r\n" +
                " \"Code\":" + code +
                "\r\n}";

            return retVal;
        }

        /// <summary>
        /// Get preview thumbs
        /// When not available a preview image will be created!
        /// </summary>
        /// <param name="originalPath"></param>
        /// <param name="originalFullPhysicalPath"></param>
        /// <returns></returns>
        private string GetPreviewImage(string originalPath, string originalFullPhysicalPath)
        {
            // get filename
            FileInfo fi = new FileInfo(originalFullPhysicalPath);
            string originalFileName = fi.Name;

            // create thumbnail filename
            string thumbFileName = GetThumbNameFromFullPhysicalPath(originalFullPhysicalPath);

            // location of thumbnail
            string newFullPhysicalPath = GetPathWithoutFilename(originalFullPhysicalPath, originalFileName) + thumbFileName;
            string newPath = GetPathWithoutFilename(originalPath, originalFileName) + thumbFileName;

            if (!File.Exists(newFullPhysicalPath))
            {
                // thumbnail maken
                ResizeImage(originalFullPhysicalPath, newFullPhysicalPath, 64, 64, false);
            }

            return newPath;
        }

        /// <summary>
        /// Change a filename, based on the full path, to a thumbnail filename
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string GetThumbNameFromFullPhysicalPath(string path)
        {
            FileInfo fi = new FileInfo(path);
            string originalFileName = fi.Name;
            string thumbExtension = fi.Extension;
            
            return "thumb_" + originalFileName.Substring(0, originalFileName.Length - thumbExtension.Length) + ".png";
        }

        /// <summary>
        /// Get icon corresponding to the filetype / extension
        /// </summary>
        /// <param name="fileType"></param>
        /// <returns></returns>
        private string GetFileTypeIconName(string fileType)
        {
            // TODO: Herschrijven naar iets mooiser
            switch (fileType.ToLowerInvariant())
            {
                case "txt":
                case "rtf": 
                    fileType = "txt";
                    break;

                case "zip":
                    fileType = "zip";
                    break;

                case "doc":
                case "docx":
                    fileType = "doc";
                    break;

                case "xls":
                case "xlsx":
                    fileType = "xls";
                    break;

                case "pdf":
                    fileType = "pdf";
                    break;

                case "htm":
                case "html":
                    fileType = "htm";
                    break;

                case "wav":
                case "mp3":
                case "wma":
                case "mid":
                    fileType = "other_music";
                    break;

                case "avi":
                case "mpg":
                case "mpeg":
                case "wmv":
                case "mp4":
                case "mov":
                case "flv":
                    fileType = "other_movie";
                    break;

                case "swf":
                    fileType = "swf";
                    break;


                default:
                    fileType = "default";
                    break;
            }

            return fileType;
        }

        private string GetInfo()
        {
            if (Request.QueryString["path"] == null)
            {
                return "";
            }

            string path = Request.QueryString["path"];
            string fullPhysicalPath = Server.MapPath(BaseUrl + path);

            return GetInfo(path, fullPhysicalPath);

        }

        /// <summary>
        /// Get folder data (files and directories)
        /// </summary>
        /// <returns></returns>
        private string GetFolder()
        {
            if (Request.QueryString["path"] == null)
            {
                return "";
            }

            string retVal = "";
            string path = Request.QueryString["path"];
            
            if (!path.EndsWith("/"))
            {
                path += "/";
            }

            string fullPhysicalPath = Server.MapPath(BaseUrl + path);

            if (Directory.Exists(fullPhysicalPath))
            {

                DirectoryInfo inputDi = new DirectoryInfo(fullPhysicalPath);

                DirectoryInfo[] subDirs = inputDi.GetDirectories();
                foreach (DirectoryInfo di in subDirs)
                {
                    string subPath = path + di.Name + "/";
                    string dirInfo = GetInfo(subPath, di.FullName);

                    if (retVal.Length > 0)
                    {
                        retVal += ",\r\n";
                    }
                    retVal += dirInfo;
                }

                FileInfo[] fileInfos = inputDi.GetFiles();

                foreach (FileInfo fi in fileInfos)
                {
                    // if file does not starts with "thumb_" is it the original file
                    // so lets get the info!
                    if(!fi.Name.StartsWith("thumb_")){
                        string subPath = path + fi.Name;
                        string fileInfo = GetInfo(subPath, fi.FullName);

                        if (retVal.Length > 0)
                        {
                            retVal += ",\r\n";
                        }

                        retVal += fileInfo;
                    }
                }
            }

            retVal = "[\r\n" + retVal + "\r\n]";

            return retVal;
        }

        /// <summary>
        /// Rename file / folder
        /// </summary>
        /// <returns></returns>
        private string Rename()
        {
            if (Request.QueryString["old"] == null)
            {
                return "";
            }

            if (Request.QueryString["new"] == null)
            {
                return "";
            }


            string path = Request.QueryString["old"];
            string fullPhysicalPath = Server.MapPath(BaseUrl + path);

            string oldPath = path;
            string oldName = "";
            string newPath = "";
            string newName = Request.QueryString["new"];
            string error = "";
            string code = "0";

            if (File.Exists(fullPhysicalPath))
            {
                // file
                FileInfo fi = new FileInfo(fullPhysicalPath);

                string dir = fi.DirectoryName;
                oldName = fi.Name;

                if (path.EndsWith("/"))
                {
                    path = path.TrimEnd('/');
                }

                newPath = GetPathWithoutFilename(path, oldName) + newName;

                if (dir != null && !dir.EndsWith("\\"))
                {
                    dir += "\\";
                }

                string fullPhysicalNewPath = dir + newName;

                if (File.Exists(fullPhysicalNewPath))
                {
                    error = "New name exists";
                    code = "-1";
                }
                else
                {
                    // rename file
                    File.Move(fullPhysicalPath, fullPhysicalNewPath);

                    // create thumbnail filename
                    string thumbOldFileName = GetThumbNameFromFullPhysicalPath(fullPhysicalPath);
                    string thumbNewFileName = "thumb_" + newName.Substring(0, newName.Length - GetFileExtension(newName).Length) + "png";
                    string oldThumbFullPhysicalPath = GetPathWithoutFilename(fullPhysicalPath, oldName) + thumbOldFileName;
                    string newThumbFullPhysicalPath = GetPathWithoutFilename(fullPhysicalPath, oldName) + thumbNewFileName;

                    if (File.Exists(oldThumbFullPhysicalPath))
                    {
                        File.Move(oldThumbFullPhysicalPath, newThumbFullPhysicalPath);
                    }
                }
            }
            else if (Directory.Exists(fullPhysicalPath))
            {
                // directory
                DirectoryInfo di = new DirectoryInfo(fullPhysicalPath);

                string dir = di.Parent.FullName;
                oldName = di.Name;
                
                if (path.EndsWith("/"))
                {
                    path = path.TrimEnd('/');
                }

                newPath = GetPathWithoutFilename(path, oldName) + newName;

                if (!dir.EndsWith("\\")) dir += "\\";
                string fullPhysicalNewPath = dir + newName;

                if (Directory.Exists(fullPhysicalNewPath))
                {
                    error = "New name exists";
                    code = "-1";
                }
                else
                    Directory.Move(fullPhysicalPath, fullPhysicalNewPath);
            }
            else
            {
                // not file or directory
                error = "no file or directory";
                code = "-1";
            }

            string retVal = "{ \"Error\":" + EnquoteJson(error) + "," +
                " \"Code\":" + code + "," +
                " \"Old Path\":" + EnquoteJson(oldPath) + "," +
                " \"Old Name\":" + EnquoteJson(oldName) + "," +
                " \"New Path\":" + EnquoteJson(newPath) + "," +
                " \"New Name\":" + EnquoteJson(newName) +
                "}";

            return retVal;
        }

        /// <summary>
        /// Delete file / folder
        /// </summary>
        /// <returns></returns>
        private string Delete()
        {
            if (Request.QueryString["path"] == null) return "";


            string path = Request.QueryString["path"];
            string fullPhysicalPath = Server.MapPath(BaseUrl + path);

            string error = "";
            string code = "0";

            if (File.Exists(fullPhysicalPath))
            {
                // get file name
                FileInfo fi = new FileInfo(fullPhysicalPath);
                string fileName = fi.Name;
                 
                try
                {
                    // delete original file
                    File.Delete(fullPhysicalPath);
                    
                    // delete thumbnail
                    string thumbFullPhysicalPath = GetPathWithoutFilename(fullPhysicalPath, fileName) + GetThumbNameFromFullPhysicalPath(fullPhysicalPath);

                    if (File.Exists(thumbFullPhysicalPath))
                    {
                        try
                        {
                            File.Delete(thumbFullPhysicalPath);
                        }
                        catch
                        {
                            error = "System can't delete thumbnail... You should not see this warning!";
                            code = "-1";
                        }
                    }
                }catch
                {
                    // can't delete original file
                    error = "Can't delete file";
                    code = "-1";
                }
            }
            else if (Directory.Exists(fullPhysicalPath))
            {
                // directory
                Directory.Delete(fullPhysicalPath, true);
            }
            else
            {
                // not file or directory
                error = "no file or directory";
                code = "-1";
            }

            string retVal = "{ \"Error\":" + EnquoteJson(error) + "," +
                " \"Code\":" + code + "," +
                " \"Path\":" + EnquoteJson(path) +
                "}";

            return retVal;
        }

        /// <summary>
        /// Add file to folder
        /// </summary>
        /// <returns></returns>
        private string Add()
        {
            string path = Request.Form["currentpath"];
            string fullPhysicalPath = Server.MapPath(BaseUrl + path);

            //WriteToLog("filemanager", "path:" + path + "\n");
            //WriteToLog("filemanager", "fullPhysicalPath:" + fullPhysicalPath + "\n");

            string retVal = "";
            string name = "";
            string ext = "";
            string error = "";
            string code = "0";

            if (Directory.Exists(fullPhysicalPath))
            {
                FileInfo fi = new FileInfo(fullPhysicalPath);
                string fileExt = fi.Extension.Replace(".", "");

                if (Request.Files["newfile"] != null)
                {
                    System.Web.HttpPostedFile inpFile = Request.Files["newfile"];
                    name = GetShortFileName(inpFile.FileName);
                    ext = GetFileExtension(name);

                    //WriteToLog("filemanager", "File Extension:" + ext + "\n");

                    if(IsAllowedExtension(ext))
                    {

                        char[] invalidFileChars = System.IO.Path.GetInvalidFileNameChars();

                        foreach (char c in invalidFileChars)
                        {
                            name = name.Replace(c.ToString(), "");
                        }

                        if (!fullPhysicalPath.EndsWith("\\"))
                        {
                            fullPhysicalPath += "\\";
                        }

                        fullPhysicalPath += name;

                        // rename existing file name
                        if (File.Exists(fullPhysicalPath))
                        {
                            if(string.IsNullOrEmpty(ExistingFileOption) || ExistingFileOption == "replace")
                            {
                                // replace current file
                                File.Delete(fullPhysicalPath);
                            }else if(ExistingFileOption == "rename")
                            {
                                // rename current filename
                                // and place the uploaded file on its place
                                File.Move(fullPhysicalPath, CreateUniqueFilename(fullPhysicalPath));
                            }
                        }

                        // save uploaded file
                        inpFile.SaveAs(fullPhysicalPath);
                    }
                    else
                    {
                        // File type not allowed
                        error = "File type not allowed";
                        code = "-1";
                    }
                }
                else
                {
                    // not file uploaded
                    error = "No file uploaded";
                    code = "-1";
                }
            }
            else
            {
                // not file or directory
                error = "No directory";
                code = "-1";
            }

            retVal = "<textarea>{ " +
                " \"Path\":" + EnquoteJson(path) + "," +
                " \"Name\":" + EnquoteJson(name) + "," +
                " \"Error\":" + EnquoteJson(error) + "," +
                " \"Code\":" + code +
                "}</textarea>";

            return retVal;

        }

        /// <summary>
        /// Add folder
        /// </summary>
        /// <returns></returns>
        private string AddFolder()
        {
            if (Request.QueryString["path"] == null)
            {
                return "";
            }

            string path = Request.QueryString["path"];
            string fullPhysicalPath = Server.MapPath(BaseUrl + path);
            string parent = path;
            string name = Request.QueryString["name"];
            string error = "";
            string code = "0";

            if (Directory.Exists(fullPhysicalPath))
            {
                // directory

                if (!fullPhysicalPath.EndsWith("\\")) fullPhysicalPath += "\\";
                fullPhysicalPath += name;

                if (!Directory.Exists(fullPhysicalPath))
                {
                    Directory.CreateDirectory(fullPhysicalPath);
                }
                else
                {
                    // not file or directory
                    error = "Name already exists";
                    code = "-1";
                }
            }
            else
            {
                // not file or directory
                error = "no file or directory";
                code = "-1";
            }

            string retVal = "{ " +
                " \"Parent\":" + EnquoteJson(parent) + "," +
                " \"Name\":" + EnquoteJson(name) + "," +
                " \"Error\":" + EnquoteJson(error) + "," +
                " \"Code\":" + code +
                "}";

            return retVal;
        }

        /// <summary>
        /// Quick upload documents
        /// </summary>
        /// <remarks>
        /// This is used by the (F)CKeditor without interaction of the the filemanger.js
        /// CK Editor part is untested at the moment
        /// Error messages - FCK documentation: http://docs.cksource.com/FCKeditor_2.x/Developers_Guide/Server_Side_Integration#Quick_Uploader
        /// Error messages - CK: http://zerokspot.com/weblog/2009/09/09/custom-filebrowser-callbacks-ckeditor/
        /// </remarks>
        /// <returns></returns>
        private string QuickUpload()
        {
            // editor version (FCK or CK)
            if (Request.QueryString["version"] == null)
            {
                return "";
            }

            // data back to editor
            string returnScript = "";

            // editor version
            string editorVersion = Request.QueryString["version"].ToLowerInvariant();

            // store directory quick uploads
            string fullPhysicalPath = Server.MapPath(BaseQuickUploadLocation);

            //check if quick upload directory exists
            if (!Directory.Exists(fullPhysicalPath))
            {
                try
                {
                    Directory.CreateDirectory(fullPhysicalPath);
                }catch
                {
                    return "";
                }
            }

            string inputFieldName;
            // get the correct "browse" input name
            // CK and FCK are using different names
            switch (editorVersion)
            {
                case "ck":
                    inputFieldName = "input";
                    break;

                case "fck":
                    inputFieldName = "NewFile";
                    break;

                default:
                    inputFieldName = "";
                    break;
            }

            if(string.IsNullOrEmpty(inputFieldName))
            {
                // send needed data back to editor
                return CreateQuickUploadMessage(editorVersion, 1, null, null, "No file uploaded");
            }

            // get the uploaded file
            System.Web.HttpPostedFile inpFile = Request.Files[inputFieldName];
            string name = GetShortFileName(inpFile.FileName);
            string ext = GetFileExtension(name);

            // are you allowed to save the file?
            if (IsAllowedExtension(ext))
            {

                // replace invalid characters in filename
                char[] invalidFileChars = System.IO.Path.GetInvalidFileNameChars();

                foreach (char c in invalidFileChars)
                {
                    name = name.Replace(c.ToString(), "");
                }

                if (!fullPhysicalPath.EndsWith("\\"))
                {
                    fullPhysicalPath += "\\";
                }

                // create full physical path with uploaded file name
                fullPhysicalPath += name;

                // is the file already on disk?
                if (File.Exists(fullPhysicalPath))
                {
                    // delete current file
                    File.Delete(fullPhysicalPath);
                }

                // save uploaded file
                try
                {
                    inpFile.SaveAs(fullPhysicalPath);

                    returnScript = CreateQuickUploadMessage(editorVersion, 0, BaseQuickUploadLocation, name, null);
                }
                catch (Exception)
                {
                    returnScript = CreateQuickUploadMessage(editorVersion, 203, null, null, "Not enough permission to write in the server");
                }
            }
            
            return returnScript;
        }

        /// <summary>
        /// Crop image
        /// </summary>
        /// <returns></returns>
        private string Crop()
        {
            if (Request.QueryString["path"] == null)
            {
                return "";
            }

            int x1 = Int32.Parse(Request.QueryString["x1"]);
            int y1 = Int32.Parse(Request.QueryString["y1"]);
            int width = Int32.Parse(Request.QueryString["width"]);
            int height = Int32.Parse(Request.QueryString["height"]);

            string path = Request.QueryString["path"];
            string fullPhysicalPath = Server.MapPath(BaseUrl + path);

            string error = "";
            string code = "0";

            if(width == 0 || height == 0)
            {
                error = "Can't crop image, dimension problem";
                code = "-1";
            }else
            {

                if (File.Exists(fullPhysicalPath))
                {
                    // create new file name
                    FileInfo fi = new FileInfo(fullPhysicalPath);
                    string fileExt = fi.Extension;
                    string fullPhysicalPathCroppedImage = GetPathWithoutFilename(fullPhysicalPath, fileExt) + "-crop.jpg";

                    if (IsImageExtension(fileExt.Replace(".", "")))
                    {
                        // crop image
                        try
                        {
                            Bitmap source = new Bitmap(fullPhysicalPath);
                            Rectangle section = new Rectangle(new Point(x1, y1), new Size(width, height));
                            Bitmap croppedImage = CropImage(source, section);

                            croppedImage.Save(fullPhysicalPathCroppedImage, ImageFormat.Jpeg);

                            // Delete thumb of current image
                            // only used when you are cropping the same image that was already cropped
                            try
                            {
                                // get cropped image name
                                string croppedImageName = GetThumbNameFromFullPhysicalPath(fullPhysicalPathCroppedImage);

                                // create full path with thumbnail name of cropped image.
                                string croppedImageFullPhysicalPath = GetPathWithoutFilename(fullPhysicalPathCroppedImage,
                                                                                                GetShortFileName(fullPhysicalPathCroppedImage)) + croppedImageName;

                                if (File.Exists(croppedImageFullPhysicalPath))
                                {
                                    try
                                    {
                                        File.Delete(croppedImageFullPhysicalPath);
                                    }
                                    catch
                                    {
                                        error = "System can't delete thumbnail... You should not see this warning!";
                                        code = "-1";
                                    }
                                }
                            }
                            catch
                            {
                                // can't delete original file
                                error = "Can't delete file";
                                code = "-1";
                            }

                        }
                        catch
                        {
                            error = "Can't crop image";
                            code = "-1";
                        }
                    }
                    else
                    {
                        error = "No (supported) image";
                        code = "-1";
                    }
                }
                else
                {
                    error = "Can't find image";
                    code = "-1";
                }
            }
            string retVal = "{ \"Error\":" + EnquoteJson(error) + "," +
                " \"Code\":" + code + "," +
                " \"Path\":" + EnquoteJson(path) +
                "}";

            return retVal;
        }

        /// <summary>
        /// Resize image
        /// </summary>
        /// <returns></returns>
        private string Resize()
        {
            if (Request.QueryString["path"] == null)
            {
                return "";
            }

            int width = Int32.Parse(Request.QueryString["width"]);
            int height = Int32.Parse(Request.QueryString["height"]);

            string path = Request.QueryString["path"];
            string fullPhysicalPath = Server.MapPath(BaseUrl + path);

            string error = "";
            string code = "0";

            if (width == 0 || height == 0)
            {
                error = "Can't resize image, dimension problem";
                code = "-1";
            }
            else
            {

                if (File.Exists(fullPhysicalPath))
                {
                    // create new file name
                    FileInfo fi = new FileInfo(fullPhysicalPath);
                    string fileExt = fi.Extension;


                    if (IsImageExtension(fileExt.Replace(".", "")))
                    {
                        // resize image
                        try
                        {
                            Image source = new Bitmap(fullPhysicalPath);
                            Image resizedImage = ResizeImage(source, width, height);

                            resizedImage.Save(GetPathWithoutFilename(fullPhysicalPath, fileExt) + "-resized.jpg",
                                              ImageFormat.Jpeg);

                        }
                        catch
                        {
                            error = "Can't resize image";
                            code = "-1";
                        }
                    }
                    else
                    {
                        error = "No (supported) image";
                        code = "-1";
                    }
                }
                else
                {
                    error = "Can't find image";
                    code = "-1";
                }
            }
            string retVal = "{ \"Error\":" + EnquoteJson(error) + "," +
                " \"Code\":" + code + "," +
                " \"Path\":" + EnquoteJson(path) +
                "}";

            return retVal;
        }

        /// <summary>
        /// Download file
        /// </summary>
        private void Download()
        {
            if (Request.QueryString["path"] == null)
            {
                return;
            }

            string path = Request.QueryString["path"];
            string fullPhysicalPath = Server.MapPath(BaseUrl + path);
            fullPhysicalPath = fullPhysicalPath.TrimEnd(new char[] { '\\', '/' });

            if (!File.Exists(fullPhysicalPath))
            {
                return;
            }


            // try to send file to user
            Response.Clear();
            
            try
            {
                Response.ContentType = "application/x-download";
                Response.AddHeader("content-disposition", "attachment; filename=" + GetShortFileName(fullPhysicalPath));
                Response.WriteFile(fullPhysicalPath);
            }
            catch
            {
                // can't send file to user
                // we send a nice error file
                byte[] buffer;
                using (var memoryStream = new MemoryStream())
                {
                    buffer = Encoding.Default.GetBytes("Sorry, you can't download this file: " + GetShortFileName(fullPhysicalPath));
                    memoryStream.Write(buffer, 0, buffer.Length);

                    Response.ContentType = "text/plain";
                    Response.AddHeader("Content-Disposition", "attachment; filename=readme.txt");
                    Response.AddHeader("Content-Length", memoryStream.Length.ToString());

                    memoryStream.WriteTo(Response.OutputStream);
                }
            }
            finally
            {
                Response.End();
            }

            return;
        }

        /// <summary>
        /// Create error JSON
        /// </summary>
        /// <param name="error"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private static string CreateError(string error, string code)
        {
            return "{ \"Error\":" + EnquoteJson(error) + ", \"Code\": " + code + " }";
        }

        /// <summary>
        /// Create error JSON, but shorter
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private static string CreateError(string error)
        {
            return CreateError(error, "-1");
        }

        /// <summary>
        /// Create feedback message for quick upload
        /// </summary>
        /// <param name="editor">Editor param</param>
        /// <param name="msgId">The error id (only for FCK editor)</param>
        /// <param name="path">Path of the file (without the filename)</param>
        /// <param name="file">Filename of uploaded file</param>
        /// <param name="feedback">Feedback message, only needed for errors</param>
        /// <returns></returns>
        private string CreateQuickUploadMessage(string editor, int msgId, string path, string file, string feedback)
        {
            string returnMessage = "";
            editor = editor.ToLowerInvariant();
            
            if(editor == "ck")
            {
                returnMessage = String.Format("<script type=\"text/javascript\">window.parent.CKEDITOR.tools.callFunction(2, '{0}', '{1});</script>",
                                                path + file, feedback);
            }

            if(editor == "fck")
            {
                returnMessage = String.Format("<script type=\"text/javascript\">window.parent.OnUploadCompleted({0},'{1}','{2}', '{3}') ;</script>",
                                                msgId, path + file, file, feedback);
            }

            return returnMessage;
        }


        ///  FUNCTION Enquote Public Domain 2002 JSON.org 
        ///  @author JSON.org 
        ///  @version 0.1 
        ///  Ported to C# by Are Bjolseth, teleplan.no 
        public static string EnquoteJson(string s)
        {
            //if (s == null || s.Length == 0)
            if(string.IsNullOrEmpty(s))
            {
                return "\"\"";
            }
            char c;
            int i;
            int len = s.Length;
            StringBuilder sb = new StringBuilder(len + 4);
            string t;

            sb.Append('"');
            for (i = 0; i < len; i += 1)
            {
                c = s[i];
                if ((c == '\\') || (c == '"') || (c == '>'))
                {
                    sb.Append('\\');
                    sb.Append(c);
                }
                else if (c == '\b')
                    sb.Append("\\b");
                else if (c == '\t')
                    sb.Append("\\t");
                else if (c == '\n')
                    sb.Append("\\n");
                else if (c == '\f')
                    sb.Append("\\f");
                else if (c == '\r')
                    sb.Append("\\r");
                else
                {
                    if (c < ' ')
                    {
                        //t = "000" + Integer.toHexString(c); 
                        string tmp = new string(c, 1);
                        t = "000" + int.Parse(tmp, System.Globalization.NumberStyles.HexNumber);
                        sb.Append("\\u" + t.Substring(t.Length - 4));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
        
        /// <summary>
        /// Check if extension is image
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        private static bool IsImageExtension(string extension)
        {
            List<string> list = new List<string>(new string[] {
                "jpg", "jpeg", "png", "gif", "bmp"
            });

            return list.Contains(extension);
        }

        /// <summary>
        /// Check if uploaded extension is allowed
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        private static bool IsAllowedExtension(string extension)
        {
            List<string> list = new List<string>(new string[] {
                "jpg", "jpeg", "png", "gif", "bmp", // afbeeldingen
                "txt", "rtf", "doc", "docx", "pdf", // documenten
                "htm", "html", // web
                "wav", "mp3", "wma", "mid", "ogg",  // audio
                "avi", "mpg", "mpeg", "wmv", "mp4", "mov", "flv", // video
                "zip", "rar", // archief
                "swf" // flash
            });

            return list.Contains(extension);
        }

        /// <summary>
        /// Get image dimensions
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        private static void GetImageDimensions(string filePath, out int height, out int width)
        {
            height = 0;
            width = 0;

            System.Drawing.Image sourceImage;

            try
            {
                sourceImage = System.Drawing.Image.FromFile(filePath);

                height = sourceImage.Height;
                width = sourceImage.Width;

                sourceImage.Dispose();
            }
            catch
            {
                // This is not a valid image. Do nothing.
            }
        }

        /// <summary>
        /// Crop image
        /// </summary>
        /// <remarks>
        /// Source: http://www.farooqazam.net/crop-image-c-sharp-and-vb-net/
        /// </remarks>
        /// <param name="source"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public Bitmap CropImage(Bitmap source, Rectangle section) {
            // An empty bitmap which will hold the cropped image  
            Bitmap bmp = new Bitmap(section.Width, section.Height);
            Graphics graphic = Graphics.FromImage(bmp);
            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic; 
            
            // Draw the given area (section) of the source image  
            // at location 0,0 on the empty bitmap (bmp)  
            graphic.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
            
            return bmp; 
        }

        /// <summary>
        /// Resize Image to new dimensions
        /// </summary>
        /// <param name="img"></param>
        /// <param name="resizedWidth"></param>
        /// <param name="resizedHeight"></param>
        /// <returns></returns>
        public Image ResizeImage(Image img, int resizedWidth, int resizedHeight)
        {
            //create a new Bitmap the size of the new image
            Bitmap bmp = new Bitmap(resizedWidth, resizedHeight);

            //create a new graphic from the Bitmap
            Graphics graphic = Graphics.FromImage((Image)bmp);
            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;

            //draw the newly resized image
            graphic.DrawImage(img, 0, 0, resizedWidth, resizedHeight);

            //dispose and free up the resources
            graphic.Dispose();

            //return the image
            return (Image)bmp;
        }

        public static string GetShortFileName(string fullFileName)
        {
            string fileName = "";
            int lastPlace = fullFileName.LastIndexOf('\\');

            if (lastPlace < 0)
            {
                lastPlace = fullFileName.LastIndexOf('/');
            }

            if (lastPlace < 0)
            {
                fileName = fullFileName;
            }
            else if (lastPlace < fullFileName.Length - 1)
            {
                fileName = fullFileName.Substring(lastPlace + 1);
            }

            return fileName;
        }

        /// <summary>
        /// Get file extension
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileExtension(string fileName)
        {
            int lastPlace = fileName.LastIndexOf('.');
            if (lastPlace < 0 || lastPlace == fileName.Length - 1) return "";

            return fileName.Substring(lastPlace + 1);
        }

        /// <summary>
        /// Remove the file name from the path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetPathWithoutFilename(string path, string filename)
        {
            return path.Substring(0, path.Length - filename.Length);
        }

        /// <summary>
        /// Created a Unique filename for the given filename
        /// </summary>
        /// <remarks>
        /// Source: http://stackoverflow.com/questions/1078003/c-how-would-you-make-a-unique-filename-by-adding-a-number/1078967#1078967
        /// </remarks>
        /// <param name="filename">A full filename, e.g., c:\temp\myfile.tmp</param>
        /// <returns>A filename like c:\temp\myfile-old-01-01-2011-01-01-30.tmp</returns>
        public string CreateUniqueFilename(string filename)
        {
            string basename = Path.Combine(
                                            Path.GetDirectoryName(filename), 
                                            Path.GetFileNameWithoutExtension(filename)
                                           );

            string uniquefilename = string.Format("{0}-old-{1}{2}", 
                                                    basename, 
                                                    String.Format("{0:dd-MM-yyyy-HH-mm-ss}", DateTime.Now),
                                                    Path.GetExtension(filename)
                                                );
            
            return uniquefilename;
        }

        /// <summary>
        /// Create thumbnail image
        /// Source: http://snippets.dzone.com/posts/show/4336
        /// </summary>
        /// <remarks>
        /// When you want to scale with cropping, this could be a nice one
        /// http://www.codeproject.com/KB/GDI-plus/imageresize.aspx
        /// </remarks>
        /// <param name="originalFile">Original file</param>
        /// <param name="newFile">New file name</param>
        /// <param name="newWidth">New width</param>
        /// <param name="maxHeight">Max height</param>
        /// <param name="onlyResizeIfWider">Only resize when image is wider</param>
        public void ResizeImage(string originalFile, string newFile, int newWidth, int maxHeight, bool onlyResizeIfWider)
        {
            Image fullSizeImage = Image.FromFile(originalFile);

            // Prevent using images internal thumbnail
            fullSizeImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
            fullSizeImage.RotateFlip(RotateFlipType.Rotate180FlipNone);

            if (onlyResizeIfWider)
            {
                if (fullSizeImage.Width <= newWidth)
                {
                    newWidth = fullSizeImage.Width;
                }
            }

            int newHeight = fullSizeImage.Height * newWidth / fullSizeImage.Width;
            if (newHeight > maxHeight)
            {
                // Resize with height instead
                newWidth = fullSizeImage.Width * maxHeight / fullSizeImage.Height;
                newHeight = maxHeight;
            }

            // do not scale when both the orginal height and width are smaller than the new height and width
            if (newWidth > fullSizeImage.Width && newHeight > fullSizeImage.Height)
            {
                newWidth = fullSizeImage.Width;
                newHeight = fullSizeImage.Height;
            }

            Image newImage = fullSizeImage.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero);

            // Clear handle to original file so that we can overwrite it if necessary
            fullSizeImage.Dispose();

            // Save resized picture
            newImage.Save(newFile, ImageFormat.Png);
        }

        /// <summary>
        /// Write data to log file
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="message"></param>
        public static void WriteToLog(string sessionId, string message)
        {
            string fileName = System.Web.HttpContext.Current.Server.MapPath("/");

            if (!fileName.EndsWith("\\")) fileName += "\\";
            fileName += sessionId + ".log";
            StreamWriter sw = new StreamWriter(fileName, true, Encoding.UTF8);
            sw.WriteLine(message);
            sw.Close();
            sw.Dispose();
        }
    }

}