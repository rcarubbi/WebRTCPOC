var ws;

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
    ws = new WebSocket(config.uri, "audio-protocol");
    ws.onmessage = function (msg) {
        if (msg.data == "Id-Received")
            postMessage(msg.data);
    }
    ws.onopen = function () {
        ws.send(config.peerId);
    };


}

function record(samples) {

    ws.send(samples);
}

function stop() {
    ws.close();
}