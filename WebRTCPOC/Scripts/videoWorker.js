var videoWs;
function dataURItoView(dataURI) {
    // convert base64 to raw binary data held in a string
    // doesn't handle URLEncoded DataURIs - see SO answer #6850276 for code that does this
    var byteString = atob(dataURI.split(',')[1]);

    var mimeString = dataURI.split(',')[0].split(':')[1].split(';')[0];
    var ab = new ArrayBuffer(byteString.length);
    var view = new DataView(ab);
    var ia = new Uint8Array(ab);
    for (var i = 0; i < byteString.length; i++) {
        view.setUint8(i, byteString.charCodeAt(i), true);
    }
    return view;
}

this.onmessage = function (e) {
    switch (e.data.command) {
        case 'init':
            init(e.data.config);
            break;
        case 'record':
            record(e.data.uri);
            break;
        case 'stop':
            stop();
            break;
    }
};

function init(config) {
    videoWs = new WebSocket(config.uri, "video-protocol");
    videoWs.onopen = function () {
        videoWs.send(config.peerId);
    };
    videoWs.onmessage = function (msg) {
        if (msg.data == "Id-Received")
        {
            postMessage(msg.data);
        }
    }
}

function record(uri) {
    var view = dataURItoView(uri);
    videoWs.send(view);
}

function stop() {
    videoWs.close();
}