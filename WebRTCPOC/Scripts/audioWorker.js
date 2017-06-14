var audioWs;

this.onmessage = function (e) {
    switch (e.data.command) {
        case 'init':
            init(e.data.config);
            break;
        case 'record':
            record(e.data.samples);
            break;
        case 'stop':
            stop();
            break;
    }
};

function init(config) {
    audioWs = new WebSocket(config.uri, "audio-protocol");
    audioWs.onmessage = function (msg) {
        if (msg.data == "Id-Received")
            postMessage(msg.data);
    }
    audioWs.onopen = function () {
        audioWs.send(config.peerId);
    };


}

function record(samples) {

    audioWs.send(samples);
}

function stop() {
    audioWs.close();
}