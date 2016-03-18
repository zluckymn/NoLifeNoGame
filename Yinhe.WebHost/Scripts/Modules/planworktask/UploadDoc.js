function CallUpLoadDiv(todoId) {
    box("/Persons/WorkShop/UpLoadDoc/" + todoId + "?t=" + new Date().getTime(), { boxid: 'UpLoadDiv', title: '上传工作成果', contentType: 'ajax', width: 800,
        onLoad: function(o) {
            //            o.db.find("#BtnaddGroup").click(function() {
            //                AddGroupForDtd(o.db.find("#DivaddGroup"), taskId);
            //            });
            //            o.db.find("#CloseWindow").click(function() {
            //                o.destroy();
            //            });
        }
    });
}

function jfwcount(t, n){
	var o = $("#proccount");
	if(o[0]){
		var v = o.text() + "";
		v = v == "" ? 0 : parseInt(v, 10);
		v = t == 'p' ? (v+n) : (v-n);
		v = v > 0 ?  v: 0;

		o.text(v);
	}
}

function getFileName(path){
	if(path) path = path.replace("/", "\\");
	var pos1 = path.lastIndexOf('/');
	var pos2 = path.lastIndexOf('\\');
	var pos  = Math.max(pos1, pos2)
	if( pos<0 ){
		return path;
	}else{
		return path.substring(pos+1);
	}
}
function fireChangeStat(wid){ // 第一次上传改变工作状态为进行中
return;
	var o = $("img[nst"+wid+"='"+wid+"']").first(), nextStat = "0", nst = $("#a_stat"+wid), v = $.trim(nst.text());
	if(v == '未开始'){
		nst.text("进行中");
		if(o[0]){
			nextStat = o.attr("nextStateId");
			if(nextStat == "2"){ // 变为进行中
				o.attr({"src":"/Content/Images/zh-cn/eps/btn-finish.gif", "nextStateId":"3"});
				$("#wtb" + wid).find("td:eq(3)").find("a").text("进行中");;
			}
		}
	}
}
var _ggisFolderUpload = false, _fttaskId, _fttodoId, _ftsecdPlanId, _ftClassStr, _ftobj;;
function FreeUpload(taskId, todoId, secdPlanId, ClassStr, obj) {
    _ggisFolderUpload = false;
    selFile.SelectFilesEx("上传文件", 0, 0, 0);
    _fttaskId = taskId; _fttodoId = todoId; _ftsecdPlanId = secdPlanId; _ftClassStr = ClassStr; _ftobj = obj;
}
function FreeUpload_(_astr, taskId, todoId, secdPlanId, ClassStr, obj) {
	var inpts = $("#zjl"+ClassStr).find("input[name='procsl']"), i = 0, arr = [], q;
    if (inpts.length == 0) inpts = $("input[name='procl']");
    for(; i < _astr.length; ++i){
		q = _astr[i].split("|");
        var _pth = q[0], _fname = getFileName(_pth);
		arr.push({"path":_pth});
	}
	if (arr.length > 0) {
        var path = "", name = "", taskRetFileId = 0, file = "", flag = false, x;
        for (x = 0; x < arr.length; x++) {
            path = arr[x].path;
			name = getFileName(path);
			arr[x].taskRetFileId = 0;
			inpts.each(function(){
				taskRetFileId = $(this).val();
				file = $.trim($(this).parent().next().text());
				if(name.toLowerCase() == file.toLowerCase()){
					arr[x].taskRetFileId = taskRetFileId;
					flag = true;
				}
			});
        }
		if(flag){
			var str = '<table cellpadding="0" cellspacing="0">';
			for (x = 0; x < arr.length; ++x) {
				str += '<tr><td>'+getFileName(arr[x].path)+'</td><td>';
				if(arr[x].taskRetFileId > 0){
					str += '<input type="checkbox" checked idex='+x+' taskRetFileId="'+arr[x].taskRetFileId+'" name="g-ck" />作为已存在的新版本';
				}
				str += '</td></tr>';
			}
			str += '</table>';
			(function(){
				var _arr = arr;
			box(str, {boxid:'showcf', title:'文件冲突处理', contentType:'html', submit_BtnName: "确定上传", cancel_BtnName: "取消", modal:true,
				onOpen:function(o){
					o.db.find("input[type='checkbox']").click(function(){
						var idx = parseInt($(this).attr("idex"), 10), taskRetFileId = $(this).attr("taskRetFileId");
						if($(this).attr("checked")){
							_arr[idx].taskRetFileId = taskRetFileId;
						}else{
							_arr[idx].taskRetFileId = 0;
						}
					});
				},
				submit_cb:function(o){
					UploadFilesFree(_arr, taskId, todoId, secdPlanId, ClassStr);
				}
			});
			})();
		}else{
			UploadFilesFree(arr, taskId, todoId, secdPlanId, ClassStr);
		}
	}
}

function FolderUpload(taskId, todoId, secdPlanId, ClassStr, obj) {
    _ggisFolderUpload = true;
	selFile.SelectFilesEx("文件夹拖拽上传", 0, 0, 0);
	_fttaskId = taskId; _fttodoId = todoId; _ftsecdPlanId = secdPlanId; _ftClassStr = ClassStr; _ftobj = obj;
}
function selFile::OnSaveFileList(strFile) {
	if(strFile == "") return;
	var str = strFile.split("?"), i, q, r = "";
    if (!_ggisFolderUpload) {
        FreeUpload_(str, _fttaskId, _fttodoId, _ftsecdPlanId, _ftClassStr, _ftobj);
        return false;
    }
	for(i = 0; i < str.length; ++i){
		q = str[i].split("|");
		r += "0|H|" + q[0] + "|H|" + q[1] + "|Y|";
	}
	r = r.replace(/\|Y\|$/g, "");
	$.ajax({type:'POST',
		url:'/Persons/WorkShop/CheckFileNameRepeat', data:{taskId:_fttaskId, todoId: _fttodoId, secdPlanId: _ftsecdPlanId, ClassStr: _ftClassStr, uploadFileList: r},
		success:function(dt){
			dt = dt.replace(/\\/g, "**");
			var rs = eval('('+dt+')');
			if(rs.success){
				rs.Result = rs.Result.replace(/\*\*/g, "\\");
				if(rs.IsRepeat != 0){
					var _tmp = '<table cellpadding="0" cellspacing="0">', arr = [];
					rs.Result = rs.Result.replace(/\|Y\|$/g, "");
					str = rs.Result.split("|Y|");
					for(i = 0; i < str.length; ++i){
						q = str[i].split("|H|"); arr[i] = {taskRetFileId:q[0], path:q[1], p1:q[2]};
						if(q[0] > 0){
							_tmp += '<tr><td>' + q[2] + "\\" +getFileName(q[1])+'</td><td>';
							_tmp += '<input type="checkbox" checked idex='+i+' taskRetFileId="'+q[0]+'" name="g-ck" />作为已存在的新版本';
							_tmp += '</td></tr>';
						}
					}
					_tmp += '</table>';
					(function(){
						var _arr = arr;
					box(_tmp, {boxid:'showcf', title:'文件冲突处理', contentType:'html', submit_BtnName: "确定上传", cancel_BtnName: "取消", modal:true,
						onOpen:function(o){
							o.db.find("input[type='checkbox']").click(function(){
								var idx = parseInt($(this).attr("idex"), 10), taskRetFileId = $(this).attr("taskRetFileId");
								if($(this).attr("checked")){
									_arr[idx].taskRetFileId = taskRetFileId;
								}else{
									_arr[idx].taskRetFileId = 0;
								}
							});
						},
						submit_cb:function(o){
							str = "";
							$.each(_arr, function(m, n){
								str += n.taskRetFileId + "|H|" + n.path + "|H|" + n.p1 + "|Y|";
							});
							str = str.replace(/\|Y\|$/g, "");
							SaveFolderUpload(str, _fttaskId, _fttodoId, _ftsecdPlanId, _ftClassStr);
						}
					});
					})();
				}else{
					SaveFolderUpload(rs.Result, _fttaskId, _fttodoId, _ftsecdPlanId, _ftClassStr);
				}
			}else{
				$.tmsg("m_upfile", rs.errors.msg,{infotype:2});
			}
		}
	});
}

function SaveFolderUpload(str, taskId, todoId, secdPlanId, ClassStr){
    if (str != "") {
        $.ajax({
            url: "/Persons/WorkShop/UploadFilesEx",
            type: 'post',
            data: {
                taskId: taskId,
                todoId: todoId,
                secdPlanId: secdPlanId,
                ClassStr: ClassStr,
                uploadFileList: str
            },
            success: function(data) {
                var str = data.split("|");
                var result = eval("(" + str[0] + ")");
                if (result.success) {
                    if (str.length > 1) {
                        if (str[1] != "") {
                            var files = eval("(" + str[1] + ")");
                            var len = files.length;
                            var str = "";
                            for (var i = 0; i < len; i++) {
                                var file = files[i];
                                if (str == "")
                                { str = fs.bLocalSign + "|" + file.path + "|" + file.strParam; }
                                else { str += "||" + fs.bLocalSign + "|" + file.path + "|" + file.strParam; }
                            }
                            fs.AddToListEx(str);
                        }
                    }
					fireChangeStat(todoId);
                    $.tmsg("m_upfile", "上传成功",{infotype:1});
					if(window.TaskCommitResult){
						$("#TaskCommitResult").a3Load("/Projects/Assignment/TaskCommitResult/" + taskId +"?math=" + Math.random(),function(){$(this).slideDown();});
					}else{
						$("#resultFileContaier").a3Load("/Projects/Assignment/TaskTodoExec/" + taskId + "/"+todoId+"?math=" + Math.random());
					}
					if($("#ProgressFileList")[0]) $("#ProgressFileList").a3Load("/Projects/Assignment/ProgressFileList/" + taskId + "/"+todoId+"?math=" + Math.random());
                }
                else {
                    $.tmsg("m_upfile", "上传失败",{infotype:2});
                }
            },
            error: function(err) { alert("接口出错，请检查！"); },
            dataType: "html"
        });
    }
}

function UploadFilesFree(arr, taskId, todoId, secdPlanId, ClassStr) {
    if (arr.length > 0) {
        var path = "", addcount = 0;
        for (var x = 0; x < arr.length; x++) {
            if (path != "") { path += "|Y|"; }
            path += arr[x].taskRetFileId + "|H|" + arr[x].path;
			if(arr[x].taskRetFileId == 0) addcount++;
        }
		//alert(path);
		//return;
        $.ajax({
            url: "/Persons/WorkShop/UploadFilesEx",
            type: 'post',
            data: {
                taskId: taskId,
                todoId: todoId,
                secdPlanId: secdPlanId,
                ClassStr: ClassStr,
                uploadFileList: path
            },
            success: function(data) {
                var str = data.split("|");
                var result = eval("(" + str[0] + ")");
                if (result.success) {
                    if (str.length > 1) {
                        if (str[1] != "") {
                            var files = eval("(" + str[1] + ")");
                            var len = files.length;
                            var str = "";
                            for (var i = 0; i < len; i++) {
                                var file = files[i];
                                //fs.AddToList(file.path, file.strParam);
                                if (str == "")
                                { str = fs.bLocalSign + "|" + file.path + "|" + file.strParam; }
                                else { str += "||" + fs.bLocalSign + "|" + file.path + "|" + file.strParam; }
                            }
                            fs.AddToListEx(str);
                        }
                    }
					fireChangeStat(todoId);
                    $.tmsg("m_upfile", "上传成功",{infotype:1});
                    //$("#progressFileList" + todoId).a3Load("/Persons/WorkShop/WorkProgressFileList/" + todoId + "?math=" + Math.random());
					jfwcount('p', addcount);
					if(window.TaskCommitResult){
						$("#TaskCommitResult").a3Load("/Projects/Assignment/TaskCommitResult/" + taskId +"?math=" + Math.random(),function(){$(this).slideDown();});
					}else{
						$("#resultFileContaier").a3Load("/Projects/Assignment/TaskTodoExec/" + taskId + "/"+todoId+"?math=" + Math.random());
					}
					if($("#ProgressFileList")[0]) $("#ProgressFileList").a3Load("/Projects/Assignment/ProgressFileList/" + taskId + "/"+todoId+"?math=" + Math.random());
                }
                else {
                    $.tmsg("m_upfile", "上传失败",{infotype:2});
                }
            },
            error: function(err) { alert("接口出错，请检查！"); },
            dataType: "html"
        });
    }
}

function UploadFilesOne(taskRetFileId, todoId, ext) {

    var arr = fs.GetSelectList(1, "*" + ext);

    if (arr.length > 0) {
         var path = "";
        for (var x = 0; x < arr.length; x++) {
            if (path != "") { path += "|Y|"; }
            path += arr[x].path;
        }
        $.post('/Persons/WorkShop/UploadNewVersion',
                {
                    taskRetFileId: taskRetFileId,
                    uploadFileList: path,
                    todoId: todoId
                },
                function(data) {
                    if (data.Success) {
						/*if($("#trfi"+taskRetFileId).attr("data-tof") == "0"){
							jfwcount(todoId, 'p', 1);
						}*/
                        var files = eval("(" + data.msgError + ")");
                        for (var x = 0; x < files.length; x++) {
                            var path = files[x].path.replace(/\//g, "\\");
                            fs.AddToList(path, files[x].strParam);
                        }
						fireChangeStat(todoId);
						$.tmsg("m_upfile", "上传成功",{infotype:1});
						//$("#progressFileList" + todoId).a3Load("/Persons/WorkShop/WorkProgressFileList/" + todoId + "?math=" + Math.random());
						if(window.TaskCommitResult){
							$("#TaskCommitResult").a3Load("/Projects/Assignment/TaskCommitResult/" + taskId +"?math=" + Math.random(),function(){$(this).slideDown();});
						}else{
							$("#resultFileContaier").a3Load("/Projects/Assignment/TaskTodoExec/" + taskId + "/"+todoId+"?math=" + Math.random());
						}
						if($("#ProgressFileList")[0]) $("#ProgressFileList").a3Load("/Projects/Assignment/ProgressFileList/" + taskId + "/"+todoId+"?math=" + Math.random());
                    } else {
                        top.showInfoBar(data.msgError);
                    }
                }, 'json');

    }
}