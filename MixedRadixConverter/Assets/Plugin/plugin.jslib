var plugin = {
    Fullscreen: function(mode)
    {
         U2J_FullScreen(mode);
    },
	OpenWindow: function(link) {
         var url = Pointer_stringify(link);
         document.onpointerup = function() { //Use onpointerup for touch input compatibility
             window.open(url);
             document.onpointerup = null;
         }
    }
};

mergeInto(LibraryManager.library, plugin);