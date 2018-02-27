$(document).ready(doSomething);

function doSomething() {
    var messages = new Messages();
}

function Messages() {
    this.sessionId = Math.random();
    this.$messagesInput = $("#messageinput");
    this.$messagesSubmit = $("#messagesubmit");

    this.$messagesInput.keypress((function(e) {
        if (e.which == 13) {
            this.$messagesSubmit.click();
            return false;
        }
    }).bind(this));
    this.$messagesSubmit.click(this.send.bind(this));
    $("#messagenew").click((function() {
        this.sessionId = Math.random();
    }).bind(this));
};

Messages.prototype.handleDisplayOfDialog = function(data) {
    var dialog = new Dialog(this.bot, this);
    $(".messagecontainer .messages").append(dialog.buildHtml(data));
    $('.messages')[0].scrollTop = $('.messages')[0].scrollHeight;
};

Messages.prototype.send = function() {
    var bot = new Bot();
    this.bot = bot;
    var query = this.$messagesInput.val();
    this.addOwn(query);
    bot.send(query, this.sessionId, this.processAgentResponse.bind(this));
    this.$messagesInput.val("");
};

Messages.prototype.processAgentResponse = function(data) {

    if(data.output && data.output.text) {
        this.addAgent(data.output.text);
    }
    if(data.context && data.context.dialog) {
        this.handleDisplayOfDialog(data.context.dialog);
    }

};

Messages.prototype.addOwn = function(text) {
    $(".messagecontainer .messages").append("<div class='own'>" + text + "</div>");
    $('.messages')[0].scrollTop = $('.messages')[0].scrollHeight;
};

Messages.prototype.addAgent = function(text) {
    $(".messagecontainer .messages").append("<div class='agent'>" + text + "</div>");
    $('.messages')[0].scrollTop = $('.messages')[0].scrollHeight;
};

function Bot() {

};

Bot.prototype.send = function(text, sessionId, callback) {
    $.ajax({
        type: "POST",
        url: "http://localhost:5001/api/watson/Query",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify({text: text, sessionId: sessionId, pressedButton: window.pressedButton }),
        success: function(data) {
            callback(data);
        },
        error: function() {
            setResponse("Internal Server Error");
        }
    });

    function setResponse(data) {

    }
};

function Dialog(bot, messages) {
    this.bot = bot;
    this.messages = messages;
    window.dialog = this;
}

Dialog.prototype.buildHtml = function (data) {
    var html = "<div class='dialog'>";
    for(var i = 0; i < data.elements.length; i++) {
        var e = data.elements[i];
        switch (e.type) {
            case "button":  html += this.buildButton(e); break;
            case "textinput": html += this.buildTextInput(e); break;
            case "textpanel": html += this.buildPanel(e); break;
        }
    }
    html += "</div>";

    return html;
};

Dialog.prototype.buildButton = function(button) {
    return '<div class="element button"><a href name="' + button.name + '">' + button.text + '</a></div>' +
        '<script type="text/javascript">' + button.onClick + '$(\'a[name="' + button.name + '"]\').parent().click((function(){this.bot.send("", this.messages.sessionId, this.messages.processAgentResponse.bind(this.messages))}).bind(window.dialog));window.pressedButton=undefined;</script>';
};

Dialog.prototype.buildPanel = function(panel) {
    return '<div class="element panel">' + panel.text + '</div>';
};

Dialog.prototype.buildTextInput = function(input) {
    return '<div class="element input"><input type="text" name="' + input.name + '"/></div></div>';
};

Dialog.prototype.sendQueryOnClick = function() {

};