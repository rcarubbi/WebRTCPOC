var serverObject;

this.onmessage = function (e) {
  switch (e.data.command) {
    case 'init':
      init(e.data.config);
      break;
    case 'record':
      record(e.data.samples);
      break;
  }
};

function init(config) {
    serverObject = config.serverObject;
    serverObject.initAudio(config.id);
}

function record(samples) {
  serverObject.sendSamples(samples);
}