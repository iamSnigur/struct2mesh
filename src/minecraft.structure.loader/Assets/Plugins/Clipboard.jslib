// Safari only allows clipboard writes synchronously inside a DOM gesture handler, and Unity
// dispatches UI input a frame later. So a transparent DOM element is placed over the label:
// it copies its stored text in its own click handler, then notifies Unity.
mergeInto(LibraryManager.library, {

  $CopyOverlay: {
    element: null,
    rect: null, // normalized to canvas: { x, y, w, h }

    layout: function () {
      var el = CopyOverlay.element;
      var rect = CopyOverlay.rect;
      var canvas = document.getElementById("unity-canvas");
      if (!el || !rect || !canvas) return;

      var r = canvas.getBoundingClientRect();
      el.style.left = (r.left + rect.x * r.width) + "px";
      el.style.top = (r.top + rect.y * r.height) + "px";
      el.style.width = (rect.w * r.width) + "px";
      el.style.height = (rect.h * r.height) + "px";
    }
  },

  InitCopyOverlayInternal__deps: ["$CopyOverlay"],
  InitCopyOverlayInternal: function (textPtr, onClick) {
    var el = CopyOverlay.element;
    if (!el) {
      el = document.createElement("div");
      el.id = "copy-overlay";
      el.style.cssText = "position:fixed;cursor:pointer;z-index:10;-webkit-tap-highlight-color:transparent;";

      el.addEventListener("click", function (e) {
        var text = el.dataset.copyText || "";

        var ta = document.createElement("textarea");
        ta.value = text;
        ta.setAttribute("readonly", "");
        ta.style.cssText = "position:fixed;top:0;left:0;opacity:0;font-size:16px;";
        document.body.appendChild(ta);
        ta.focus();
        ta.select();
        ta.setSelectionRange(0, text.length); // iOS
        var ok = false;
        try { ok = document.execCommand("copy"); } catch (err) {}
        document.body.removeChild(ta);

        if (!ok && navigator.clipboard && window.isSecureContext)
          navigator.clipboard.writeText(text).catch(function (err) { console.warn("Copy failed", err); });

        var canvas = document.getElementById("unity-canvas");
        if (canvas) canvas.focus();

        var r = el.getBoundingClientRect();
        var fraction = r.width > 0 ? (e.clientX - r.left) / r.width : 0.5;
        {{{ makeDynCall('vf', 'onClick') }}}(fraction);
      });

      window.addEventListener("resize", CopyOverlay.layout);
      document.body.appendChild(el);
      CopyOverlay.element = el;
    }

    el.dataset.copyText = UTF8ToString(textPtr);
  },

  SetCopyOverlayRectInternal__deps: ["$CopyOverlay"],
  SetCopyOverlayRectInternal: function (x, y, w, h) {
    CopyOverlay.rect = { x: x, y: y, w: w, h: h };
    CopyOverlay.layout();
  }
});
