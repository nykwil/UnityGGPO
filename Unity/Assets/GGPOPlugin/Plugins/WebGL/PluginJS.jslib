//A prefix to add to every function in the plugin
const PluginPrefix = "GGPOPlugin_";

//Plugin implementation
var Plugin = {
    confirm: function(messagePtr) {
        Static.confirmCallCount++;
        var message = Helper.ToJsString(messagePtr);
        return confirm(message);
    },
    prompt: function(messagePtr, defaultPtr) {
        var message = Helper.ToJsString(messagePtr);
        var defaultInput = Helper.ToJsString(defaultPtr);
        var result = prompt(message, defaultInput);
        if (result != null) {
            result = Helper.ToCsString(result);
        }
        return result;
    },
    getConfirmCallCount: function() {
        return Static.confirmCallCount;
    }
};

//A static variable to store data between API calls
var Static = {
    confirmCallCount: 0
}

//Helper functions
var Helper = {
    //Convert a Javascript string to a C# string
    ToCsString: function(str) {
        if (typeof str === 'object') {
            str = JSON.stringify(str);
        }
        var bufferLength = lengthBytesUTF8(str) + 1;
        var buffer = _malloc(bufferLength);
        stringToUTF8(str, buffer, bufferLength);
        return buffer;
    },

    //Convert a C# string pointer to a Javascript string
    ToJsString: function(ptr) {
        return Pointer_stringify(ptr);
    },

    //Convert a C# json string pointer to a Javascript object
    ToJsObject: function(ptr) {
        var str = Pointer_stringify(ptr);
        try {
            return JSON.parse(str);
        } catch (e) {
            return null;
        }
    },

    //free allocated memory of a C# pointer
    FreeMemory: function(ptr) {
        _free(ptr);
    }
};

//Plugin merge function
function MergePlugin(plugin, prefix) {
    //prefix
    if (prefix) {
        var prefixedPlugin = {};
        for (var key in plugin) {
            if (plugin.hasOwnProperty(key)) {
                prefixedPlugin[prefix + key] = plugin[key];
            }
        }
        plugin = prefixedPlugin;
    }
    //helper
    if (Helper) {
        plugin.$Helper = Helper;
        autoAddDeps(plugin, '$Helper');
    }
    //static vars
    if (Static) {
        plugin.$Static = Static;
        autoAddDeps(plugin, '$Static');
    }
    //merge
    mergeInto(LibraryManager.library, plugin);
}

MergePlugin(Plugin, PluginPrefix);