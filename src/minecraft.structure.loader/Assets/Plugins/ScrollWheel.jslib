mergeInto(LibraryManager.library, {

  InitScrollWheelInternal: function () {
    if (window._scrollWheelInitialized) return;
    window._scrollWheelInitialized = true;
    window._accumulatedScroll = 0;

    var canvas = document.getElementById("unity-canvas") || document;

    canvas.addEventListener("wheel", function (e) {
      var delta = e.deltaY;

      switch (e.deltaMode) {
        case 1: // DOM_DELTA_LINE (The delta values are measured in lines)
          delta *= 16;
          break;
        case 2: // DOM_DELTA_PAGE (The delta values are measured in pages)
          delta *= window.innerHeight;
          break;
        default:
          break; // DOM_DELTA_PIXEL (The delta values are measured in pixels)
      }

      var step = Math.sign(delta) * Math.min(Math.abs(delta) / 100, 1);
      window._accumulatedScroll += step;

      e.preventDefault();
    }, { passive: false });
  },

  GetScrollDeltaInternal: function () {
    var value = window._accumulatedScroll || 0;
    window._accumulatedScroll = 0;
    return -value;
  }

});
