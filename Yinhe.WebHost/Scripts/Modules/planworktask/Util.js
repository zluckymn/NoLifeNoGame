// 更改页面地址参数值，并且刷新页面
// ZWW
function setUrlParamValue(name,value){
    var url = window.location.href;
    if(url.indexOf("?")== -1){
        url += "?";
        url += name + "=" + value;
    }else{
        var paramString = url.substring(url.indexOf("?") + 1,url.length);
        var arrParam = paramString.split("&");
        var exist = false;
        for(var i=0,len=arrParam.length;i<len;i++){
            if(arrParam[i].indexOf(name + "=") != -1){
                arrParam[i] = name + "=" + value;
                exist = true;
                break;
            }
        }
        if(exist==false){
            arrParam.push(name + "=" + value);
        }
        url = url.substring(0,url.indexOf("?")) + "?" + arrParam.join("&");
    }
    window.location.href = url;
}