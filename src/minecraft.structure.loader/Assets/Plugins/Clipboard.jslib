mergeInto(LibraryManager.library, {
  CopyToClipboardInternal: function (textPtr) {
    var text = UTF8ToString(textPtr);

    function fallbackCopy() {
      var ta = document.createElement("textarea");
      ta.value = text;
      ta.style.position = "fixed";
      ta.style.opacity = "0";
      document.body.appendChild(ta);
      ta.focus();
      ta.select();
      try { 
        document.execCommand("copy"); 
      } catch (e) { 
        console.warn("Copy failed", e); 
      }
      document.body.removeChild(ta);
    }

    if (navigator.clipboard && window.isSecureContext) {
      navigator.clipboard.writeText(text).catch(fallbackCopy);
    } else {
      fallbackCopy();
    }
  }
});