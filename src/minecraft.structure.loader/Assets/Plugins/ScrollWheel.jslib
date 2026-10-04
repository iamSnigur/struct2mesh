mergeInto(LibraryManager.library, {

  InitScrollWheelInternal: function () {
    if (window._scrollWheelInitialized) return;
    window._scrollWheelInitialized = true;
    window._accumulatedScroll = 0;
  
    var ua = navigator.userAgent;
    var isMac = /Macintosh|Mac OS X/i.test(ua);
    var isSafari = /Safari/i.test(ua) && !/Chrome|Chromium|CriOS|FxiOS|Edg|OPR|Android/i.test(ua);
    var isFirefox = /Firefox/i.test(ua);
  
    // px of deltaY that count as one zoom step
    // Chrome/Edge on Windows (baseline)
    var pixelsPerStep = 100;               
    if (isSafari)
        pixelsPerStep = 50;
    else if (isMac && !isFirefox) 
        pixelsPerStep = 50; // Chrome/Edge on macOS
    
    // Firefox line mode: 3 lines per notch
    var linesPerStep = 3;                  
  
    var canvas = document.getElementById("unity-canvas") || document;

    canvas.addEventListener("wheel", function (e) {
      var step;
      switch (e.deltaMode) {
        case 1:  step = e.deltaY / linesPerStep; break; // lines
        case 2:  step = e.deltaY; break;                // pages
        default: step = e.deltaY / pixelsPerStep; break; // pixels
      }
  
      window._accumulatedScroll += Math.max(-1, Math.min(1, step));
      e.preventDefault();
    }, { passive: false });
  },

  GetScrollDeltaInternal: function () {
    var value = window._accumulatedScroll || 0;
    window._accumulatedScroll = 0;
    return -value;
  }

});
