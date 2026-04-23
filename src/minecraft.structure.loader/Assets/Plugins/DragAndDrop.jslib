mergeInto(LibraryManager.library, {

  CloseUploadPanel: function () {
    const overlay = document.getElementById("uploadOverlay");
    const fileInfo = document.getElementById("fileInfo");
    overlay.style.display = "none";
    fileInfo.innerHTML = "";
  },

  OpenUploadPanel: function () {
    if (!window.uploadInitialized) {
      window.uploadInitialized = true;

      const overlay = document.getElementById("uploadOverlay");
      const dropZone = document.getElementById("dropZone");
      const input = document.getElementById("fileInput");
      const fileInfo = document.getElementById("fileInfo");

      window._upload = { overlay, dropZone, input, fileInfo };

      overlay.onclick = function (e) {
        if (e.target !== overlay) return;

        overlay.style.display = "none";
        fileInfo.innerHTML = "";
      };

      dropZone.onclick = function () {
        input.click();
      };

      input.onchange = function () {
        handleFile(input.files[0]);
      };

      dropZone.ondragover = function (e) {
        e.preventDefault();
        dropZone.classList.add("drag");
      };

      dropZone.ondragleave = function () {
        dropZone.classList.remove("drag");
      };

      dropZone.ondrop = function (e) {
        e.preventDefault();
        dropZone.classList.remove("drag");
        handleFile(e.dataTransfer.files[0]);
      };

      function handleFile(file) {
        if (!file) return;

        fileInfo.innerHTML = file.name + " (" + (file.size / 1024).toFixed(1) + " KB)";

        const reader = new FileReader();
        reader.onload = function () {
          const payload = JSON.stringify({
            FileName: file.name,
            FileContent: arrayBufferToBase64(reader.result)
          });

          window.unityInstance.SendMessage(
            "FileReceiver",
            "OnFileLoaded",
            payload
          );
        };

        reader.readAsArrayBuffer(file);
      }

      function arrayBufferToBase64(buffer) {
        let binary = '';
        let bytes = new Uint8Array(buffer);
        let len = bytes.byteLength;
        for (let i = 0; i < len; i++) {
          binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
      }
    }

    // show panel
    window._upload.overlay.style.display = "flex";
  }

});