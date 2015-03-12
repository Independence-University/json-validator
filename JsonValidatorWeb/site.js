﻿(function (undefined) {

    var elOutput = document.getElementById("result"),
        elPre = document.querySelector("pre"),
        elInstance = document.getElementById("instance"),
        elSchema = document.getElementById("schema"),
        elForm = document.querySelector("form");

    function onSubmit(e) {
        e.preventDefault();

        var instance = elInstance.value;
        var schema = elSchema.value;

        var http = new XMLHttpRequest();
        http.open("POST", "/api/v1.ashx", true);
        http.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
        http.onreadystatechange = function () {
            if (http.readyState == 4 && http.status == 200) {
                showErrors(http.responseText, instance);
                elInstance.disabled = false;
                elSchema.disabled = false;
            }
        }
        http.send(getPostObject(instance, schema));

        elInstance.disabled = true;
        elSchema.disabled = true;
    }

    /**
     * @param {string} instance
     */
    function showErrors(result, instance) {
        var errors = JSON.parse(result);
        var string = "";

        for (var i = errors.length - 1; i >= 0; i--) {
            var error = errors[i];
            var start = error.Start;
            var tooltip = error.Message.replace(/"/gi, "&quot;");

            instance = instance.substring(0, error.Start) + "<mark title=\"" + tooltip + "\">" + instance.substring(error.Start, error.Start + error.Length) + "</mark>" + instance.substring(error.Start + error.Length);
        }

        elOutput.style.display = "block";
        elPre.innerHTML = instance;
        location.href = "#result";

        elOutput.querySelector("ol").innerHTML = errors.map(function (err) {
            return "<li><span>position:" + err.Start + ", length:" + err.Length + "</span><pre>" + err.Message + "</pre></li>"
        })

        if (errors.length > 0) {
            elOutput.firstElementChild.style.color = "red";
            elOutput.firstElementChild.innerHTML = errors.length + " error(s)";
        }
        else {
            elOutput.firstElementChild.style.color = "green";
            elOutput.firstElementChild.innerHTML = "0 errors";
        }
    }

    function getPostObject(instance, schema) {
        var obj = {
            Instance: {
                Kind: parseJson(instance) ? "Text" : "Uri",
                Value: instance
            },
            Schema: {
                Kind: parseJson(schema) ? "Text" : "Uri",
                Value: schema
            }
        }

        return JSON.stringify(obj);
    }

    function onInstanceChanged(e) {
        var instance = parseJson(elInstance.value);

        if (instance) {
            var schema = instance.$schema;

            if (schema) {
                elSchema.value = schema;
            }
        }

        if (instance || e.target.value.length < 2)
            e.target.removeAttribute("invalid");
        else
            e.target.setAttribute("invalid", "");
        //elSchema.disabled = instance !== null;
    }

    function parseJson(string) {
        try {
            return JSON.parse(string);
        }
        catch (ex) {
            return null;
        }
    }

    var tabString = "  ";
    var tabSize = tabString.length;


    function handleEditingKeys(e) {
        var evt = event || e;
        var code = evt.keyCode || e.which;
        var src = evt.srcElement;
        var selStart = src.selectionStart;
        var selEnd = src.selectionEnd;
        var text = src.value;

        switch (code) {
            case 46: //delete
                if (!evt.shiftKey) {
                    return true;
                }

                var i;
                for (i = selStart - 1; i >= 0; --i) {
                    if (text[i] === "\n") {
                        ++i;
                        var nextNewLine = text.indexOf("\n", i);
                        src.value = text.substr(0, i) + (nextNewLine > -1 ? text.substr(nextNewLine + 1) : "");
                        src.setSelectionRange(i, i);
                        break;
                    }
                }

                if (i === -1) {
                    var nextNewLine = text.indexOf("\n");

                    if (nextNewLine === -1) {
                        src.value = "";
                    } else {
                        src.value = text.substr(0, nextNewLine);
                    }

                    src.setSelectionRange(0,0);
                    break;
                }

                break;
            case 36: //home
                var i;
                for (i = selStart - 1; i >= 0; --i) {
                    if (text[i] === "\n") {
                        ++i;
                        for (; text[i] === " "; ++i);

                        if (i === selStart) {
                            return true;
                        }

                        src.setSelectionRange(i, i);
                        break;
                    }
                }

                if (i === -1) {
                    ++i;
                    for (; text[i] === " "; ++i);

                    if (i === selStart) {
                        return true;
                    }

                    src.setSelectionRange(i, i);
                    break;
                }

                break;
            case 9: //tab
                if (!evt.shiftKey) {
                    src.value = text.substr(0, selStart) + tabString + text.substr(selStart);
                    src.setSelectionRange(selStart + tabSize, selEnd + tabSize);
                } else {
                    var distance = 0;
                    for (var i = selStart - 1; i >= 0; --i) {
                        if (text[i] === "\n") {
                            var initial = i;
                            ++i;
                            for (; text[i] === " "; ++i, ++distance);
                            if (distance > 0) {
                                if (distance < tabSize) {
                                    src.value = text.substr(0, initial) + text.substr(i);
                                    src.setSelectionRange(selStart - distance, selEnd - distance);
                                } else {
                                    src.value = text.substr(0, initial + 1) + text.substr(initial + 1 + tabSize);
                                    src.setSelectionRange(selStart - tabSize, selEnd - tabSize);
                                }
                            }

                            break;
                        }
                    }

                    if (i === -1) {
                        ++i;
                        for (; text[i] === " "; ++i, ++distance);
                        if (distance > 0) {
                            if (distance < tabSize) {
                                src.value = text.substr(i);
                                src.setSelectionRange(selStart - distance, selEnd - distance);
                            } else {
                                src.value = text.substr(tabSize);
                                src.setSelectionRange(selStart - tabSize, selEnd - tabSize);
                            }
                        }

                        break;
                    }
                }
                break;
            case 13: //return
                //Determine indent
                var found = false;
                for (var i = selStart - 1; i >= 0; --i) {
                    if (text[i] === "\n") {
                        var pad = "";
                        ++i;
                        for (; text[i] === " "; ++i, pad += " ");
                        src.value = text.substr(0, selStart) + "\n" + pad + text.substr(selEnd);
                        src.setSelectionRange(selStart + pad.length + 1, selStart + pad.length + 1);
                        found = true;
                        break;
                    }
                }

                if (!found) {
                    return true;
                }

                break;
            default:
                return true;
        }

        evt.preventDefault();
        evt.cancelBubble = true;
        return false;
    }


    elForm.addEventListener("submit", onSubmit, false);
    elInstance.addEventListener("keyup", onInstanceChanged, false);
    elInstance.addEventListener("keydown", handleEditingKeys, true);
    elSchema.addEventListener("keydown", handleEditingKeys, true);

})();