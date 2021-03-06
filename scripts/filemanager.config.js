/*---------------------------------------------------------
  Configuration
---------------------------------------------------------*/

// Set culture to display localized messages
var culture = 'en';

// Set default view mode : 'grid' or 'list'
var defaultViewMode = 'grid';

// Autoload text in GUI
// If set to false, set values manually into the HTML file
var autoload = true;

// Display full path - default : false
var showFullPath = false;

// Browse only - default : false
var browseOnly = false;

// Set this to the server side language you wish to use.
// options: php, jsp, lasso, asp, cfm (download the latest version of the extenstions from the Filemanger mainbranche
var lang = 'ashx';

var am = document.location.pathname.substring(1, document.location.pathname.lastIndexOf('/') + 1);

// Set this to the directory you wish to manage.
var fileRoot = '/' + am + 'userfiles/';

// Show image previews in grid views?
var showThumbs = true;

// Show edit button, when showing you need to have the "image-editor" plugin
var showEdit = true;

// Allowed image extensions when type is 'image'
// Don't forget to change the image extension settings in the filemanger.ashx when making changes!
var imagesExt = ['jpg', 'jpeg', 'gif', 'png'];