var videoWs;
 

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
   
    videoWs.send(uri);
}

function stop() {
    videoWs.close();
}