mergeInto(LibraryManager.library, {

  CloseUploadPanel: function () {
    const overlay = document.getElementById("uploadOverlay");
    overlay.style.display = "none";
  },

  OpenUploadPanel: function () {
    const dropContent = document.getElementById("dropContent");
    const uploadContent = document.getElementById("uploadContent");
    const progressBar = document.getElementById("progressBar");
    const fileInfo = document.getElementById("fileInfo");
    
    dropContent.style.display = "block";
    uploadContent.style.display = "none";
    progressBar.style.width = "0%";
    fileInfo.innerHTML = "";
          
    if (!window.uploadInitialized) {
      window.uploadInitialized = true;

      const overlay = document.getElementById("uploadOverlay");
      const dropZone = document.getElementById("dropZone");
      const input = document.getElementById("fileInput");

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

        dropContent.style.display = "none";
        uploadContent.style.display = "block";
        
        fileInfo.innerHTML = file.name + " (" + (file.size / 1024).toFixed(1) + " KB)";
        progressBar.style.width = "0%";
        
        let fakeProgress = 0;
        let fileLoaded = false;
        let minTimePassed = false;
        let pendingPayload = null;

        const fakeInterval = setInterval(() => {
          if (fakeProgress < 90) {
            fakeProgress += 0.6;
            progressBar.style.width = fakeProgress + "%";
          }
        }, 30);
        
        setTimeout(() => {
          minTimePassed = true;
          if (fileLoaded) finish();
        }, 800);

        const finish = () => {
          clearInterval(fakeInterval);
          progressBar.style.width = "100%";
          
          setTimeout(() => {
            if (pendingPayload) {
              window.unityInstance.SendMessage("FileReceiver", "OnFileLoaded", pendingPayload);
              pendingPayload = null;
            }
          }, 100);
        }; 

        const reader = new FileReader();
        
        reader.onprogress = (e) => {
          if (e.lengthComputable) {
            const real = (e.loaded / e.total) * 100;
            if (real > fakeProgress) {
              fakeProgress = real;
              progressBar.style.width = fakeProgress + "%";
            }
          }
        };
                
        reader.onload = function () {
          pendingPayload = JSON.stringify({
            FileName: file.name,
            FileContent: arrayBufferToBase64(reader.result)
          });

          fileLoaded = true;
          if (minTimePassed) finish();
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