mergeInto(LibraryManager.library, {
    UploadFile: function() {
        var input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.onchange = e => { 
            var file = e.target.files[0]; 
            var reader = new FileReader();
            reader.onload = function(event) {
                var dataUrl = event.target.result;
                var base64Data = dataUrl.split(',')[1];
                SendImageToUnity(base64Data);
            }
            reader.readAsDataURL(file);
        }
        input.click();
    }
});

function SendImageToUnity(base64Data) {
    // Remplacez `unityInstance` par la référence correcte à l'instance Unity
    unityInstance.BridgeCallback('GameManager', 'OnFileUploaded', base64Data);
}