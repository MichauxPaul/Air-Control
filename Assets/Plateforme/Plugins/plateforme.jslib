mergeInto(LibraryManager.library, {
  InitApp: function () {
    window.initAppCallback();
  },
  InitGame: function () {
    window.initGameCallback();
  },
  SaveHighscore: function (score) {
    window.saveHighscoreCallback(score);
  },
  GetHighscores: function () {
    window.getHighscoresCallback();
  },
  Close: function () {
    window.closeCallback();
  },
  Restart: function () {
    window.restartCallback();
  },
  FullscreenSwitch: function (fullscreenState) {
    window.fullscreenSwitchCallback(fullscreenState);
  }
});