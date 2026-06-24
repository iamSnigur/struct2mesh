mergeInto(LibraryManager.library, {

    DownloadFile: function(namePtr, dataPtr, length)
    {
        const fileName = UTF8ToString(namePtr);

        const bytes = HEAPU8.slice(
            dataPtr,
            dataPtr + length
        );

        const blob = new Blob(
            [bytes],
            { type: "application/zip" }
        );

        const url =
            URL.createObjectURL(blob);

        const a =
            document.createElement("a");

        a.href = url;
        a.download = fileName;
        a.click();

        URL.revokeObjectURL(url);
    }
});