

   function uploadObj_OnAdd(strFileName, fileId, param,lErrorCode)
        if fileId="" then
           RemoveUploadItem strFileName, param  
           'CancelFileUpload(param)
           
       select case lErrorCode
       case -1
       hiAlert "未知错误","添加文件上传任务失败" 
       case 1
       hiAlert "登陆失败","添加文件上传任务失败" 
       case 2
       hiAlert "资源没有找到","添加文件上传任务失败" 
       case 3
       hiAlert "FS不在线","添加文件上传任务失败" 
       case 4
       hiAlert "上传文件，服务器文件保存失败","添加文件上传任务失败" 
       case 5
       hiAlert "服务器上的文件还不完整","添加文件上传任务失败" 
       case 6
       hiAlert "FS未注册","添加文件上传任务失败" 
       case 7
       'msgbox "文件读错误"
       if mutiselect.FileExists(strFileName) = false then
               hiAlert "请检查文件是否存在:"&strFileName,"要上传的文件不存在" 
               else
               hiAlert "请关闭打开文件的应用程序再上传:"&strFileName,"添加上传文件" 
               end if
               case 8
       hiAlert "FS不存在","添加文件上传任务失败" 
       case 9
       hiAlert "FS已经激活","添加文件上传任务失败" 
       case 10
       hiAlert "FS密码错误","添加文件上传任务失败" 
   end select
   end if

    'E_UnknowError		= -1,
	'E_NoError		= 0,
	'E_LoginFaild,				1// 登陆失败
	'E_ResNotFound,				2// 资源没有找到
	'E_FSOffline,				3// FS不在线
	'E_FileSave,				4// 上传文件，服务器文件保存失败
	'E_FileNotComplete,			5// 服务器上的文件还不完整
	'E_FSLoginFaild,			6	// FS未注册
	'E_ReadFile,				7// 文件读错误
	'E_FSNotFound,				8// FS不存在
	'E_Aleady_Active,			9// FS已经激活
	'E_Password_Faild,			10// FS密码错误
	end  function
	
	function uploadObj_OnFinished(filepath, result, param)
		uploadObj_OnTaskFinished filepath, result, param
	end  function
	
	function uploadObj_OnError(strFileName,a,param,lErrorCode)
		'MsgBox strFileName & "任务错误"
		   select case lErrorCode
               case -1
               hiAlert "未知错误","上传失败"
               case 1
               hiAlert "登陆失败","上传失败"
               case 2
               hiAlert "资源没有找到","上传失败"
               case 3
               hiAlert "FS不在线","上传失败"
               case 4
               hiAlert "上传文件，服务器文件保存失败","上传失败"
               case 5
               hiAlert "服务器上的文件还不完整","上传失败"
               case 6
               hiAlert "FS未注册","上传失败"
               case 7
               'msgbox "文件读错误"
               if mutiselect.FileExists(strFileName) = false then
               hiAlert "请检查文件是否存在:"&strFileName,"要上传的文件不存在" 
               else
               hiAlert "请关闭打开文件的应用程序再上传:"&strFileName,"上传过程中" 
               end if
               case 8
               hiAlert "FS不存在","上传失败"
               case 9
               hiAlert "FS已经激活","上传失败"
               case 10
               hiAlert "FS密码错误","上传失败"
               end select
	end  function	
	
	'下载事件
	function readObj_OnAdd(strFileID, strFileName, ret)
		'MsgBox strFileID & "  " & strFileName & " 下载  " & ret
	end function
	function readObj_OnError(strFileID, strFileName, ret)
		MsgBox "文件不存在，请联系文件创建人重新上传！错误代码：" & ret,vbYes,"提示信息"
	end function
	function readObj_OnFinished(result, strFileName)
		'MsgBox strFileID & "  " & strFileName & "下载出错:"
		readObj_Finished result, strFileName
	end function
	'上传进度回发事件
	function readObj_OnProgress(strFileID, strFileName,fProgress,ullFileSize)
	    readfileProgress fProgress,strFileName,ullFileSize
		'MsgBox strFileID & "  " & strFileName & "下载出错:" & ret 
		'readObj_Finished strFileID, strFileName
	end function
	
	'dwg图分割完成回发事件
	function spliteObj_OnFinished(filename, param, result, count)
		'MsgBox filename & " 分割完成 " & param & "返回结果" & result & " 个数:" &count
		spliteObj_Finished filename, param, result, count
	end function
	'提取缩略图事件
	function thumbObj_OnFinished(filename, lWidth, lHeight, bstrParam, bstrOutFileName)
		'MsgBox filename & " 提取缩略图: " & bstrOutFileName & ", " & lWidth & "*" & lHeight & " 参数 " &  bstrParam
		thumbObj_Finished filename, lWidth, lHeight, bstrParam, bstrOutFileName
	end function
	
	function downloadObj_OnFinished(hash,path)
	chen(path)
	end function 
	

	
	
	
Dim bDocOpen

 Sub oframe_OnDocumentOpened(str, obj)
     bDocOpen = True
       	 on error resume next
     oframe.ListenEvents
 End Sub

 Sub oframe_OnDocumentClosed()
  	 bDocOpen = False
  	 on error resume next
  	 oframe.UnListenEvents
End Sub

Sub oframe_OnFileCommand(Item, bCancel)
if Item = 3 or Item = 4 then
bCancel = true
msgbox "当前系统禁止在此处保存文档，请点击网页顶部的保存按钮进行保存"
end if
End Sub

sub OnSaveCompleted(a,b,c)
msgbox 1
end sub

 Sub NewDoc()
   On Error Resume Next
   oframe.showdialog 0 'dsoDialogNew
   if err.number then
      MsgBox "Unable to Create New Object: " & err.description
   end if
 End Sub

 Sub OpenDoc()
   On Error Resume Next
   oframe.showdialog 1 'dsoDialogOpen
    if err.number then
      MsgBox "Unable to Open Document: " & err.description
   end If
   

 End Sub

Sub SaveDoc(Dir,tempDir,p,V)
   On Error Resume Next
   if oframe.IsDirty then
    oframe.Save Dir,true
    oframe.Save tempDir,true
    'oframe.close
    uploadObj.AddTask V,Dir,"ShowDoc@" + p
    msgbox "保存文件上传成功！",vbYes,"提示信息"
    oframe.Activate
    else
    oframe.Activate
    end if
end sub
 

 Sub SaveCopyDoc()
   On Error Resume Next
   'If Not bDocOpen Then
      'MsgBox "You do not have a document open."
   'Else
      oframe.showdialog 3 'dsoDialogSaveCopy
   'End If
 End Sub

 Sub ChgLayout()
   On Error Resume Next
   If Not bDocOpen Then
      MsgBox "You do not have a document open."
   Else
      oframe.showdialog 5 'dsoDialogPageSetup
   End If
 End Sub

 Sub PrintDoc()
   On Error Resume Next
   'If Not bDocOpen Then
      'MsgBox "You do not have a document open."
   'Else
      oframe.showdialog 4
   'End If
 End Sub

 Sub CloseDoc()
   On Error Resume Next
   If Not bDocOpen Then
      MsgBox "You do not have a document open."
   Else
      oframe.close
   End If
 End Sub

 Sub ToggleTitlebar()
   Dim x
   On Error Resume Next
   x = oframe.Titlebar
   oframe.Titlebar = Not x
 End Sub

 Sub ToggleToolbars()
   Dim x
   On Error Resume Next
   x = oframe.Toolbars
   oframe.Toolbars = Not x
 End Sub

Sub chen(url) 'onload时调用的方法
on error resume next



	'ToggleToolbars
	oframe.EnableFileCommand(0) = False
    oframe.EnableFileCommand(1) = False
    oframe.EnableFileCommand(2) = False
    'oframe.EnableFileCommand(3) = False
    'oframe.EnableFileCommand(4) = False


	'ToggleTitlebar
	on error resume next
	oframe.ActivationPolicy=1
	oframe.FrameHookPolicy=1
	oframe.open url
	oframe.Activate
	
	if err.number <> 0 then
	oframe.FrameHookPolicy=0
	oframe.open url,True
	oframe.Activate
	Err.Clear
	end if
	
end sub

sub Doactive()
on error resume next
oframe.Activate
end sub