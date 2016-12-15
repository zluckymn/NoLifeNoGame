<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/BootStrap.Master" Inherits="System.Web.Mvc.ViewPage"  %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
   <meta charset="utf-8" >
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <!-- 上述3个meta标签*必须*放在最前面，任何其他内容都*必须*跟随其后！ -->
    <meta name="description" content="">
    <meta name="author" content="">

  
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
 
<div class="container">

      <div class="page-header">
        <h1><strong>专用试题库</strong><small><a href="/QuestionAnswer/Index">返回</a></small></h1>
         <small><a href="/QuestionAnswer/QuestionTest">模拟考试</a></small>
         <small><a href="/QuestionAnswer/QuestionTest/?questionCount=1&quickMode=1">十秒答题</a></small>
         <button class="btn btn-lg btn-primary navbar-fixed-top" type="button" id="answerBtn" onclick='CheckAnswer()'>查看/隐藏答案</button>
        </div>

<%
    string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
    DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
    var libId = PageReq.GetParam("libId");
    var allQuestionList = dataOp.FindAllByQuery("WPM_Question", Query.EQ("libId", libId)).OrderBy(c=>c.Date("createDate")).ToList();
     %>
<br />
 
 
 <span class=" navbar-fixed-bottom " style=" float:right">
        <a href="#Singel">单选</a>  
         <a href="#Mutiple">多选</a> 
         <a href="#YesOrNO">对错</a> 
      <a href="#Instance">案例</a>  
 </span>
 
<%
  var index=1;
  var groupIndex = 1;
  var group = string.Empty;
  var groupName = string.Empty;

  foreach (var question in allQuestionList)
  {
      var selectType = "radio";
      var otherGroup=false;
      
       
       switch (question.Text("group"))
          {
              case "Mutiple": groupName = "多选题"; selectType = "checkbox"; break;
              case "YesOrNO": groupName = "对错题"; break;
              case "Singel": groupName = "单选题"; break;
              case "Instance": groupName = "案例题"; break;
          }
      if (question.Text("group") != group)
      {
          group = question.Text("group");
          otherGroup=true;
          
      }
     
      var optionList = question.BsonDocumentList("OptionList")!=null?question.BsonDocumentList("OptionList").FirstOrDefault():null;
      if (optionList == null) continue;
        %>
        <%if(otherGroup){ %>
        <h1 id="<%=question.Text("group") %>"><%=groupIndex++%>、<%=groupName%></h1>
        <%} %>
   <div class="panel panel-default" name="question" questionId="<%=question.Text("questionId")%>"  answer="<%=question.Text("answer")%>">
      
   <div class="panel-heading" >
              <label><h3 id="title_<%=question.Text("questionId")%>" class="panel-title"></label><strong><%=index++%>.<%=question.Text("question")%> </strong>
              <span name="answer" style=" display:none;text-decoration:underline"><%=question.Text("answer")%></span><br/>
              </h3>
    </div>
    <div class="panel-body" >
    <% 
        foreach(var column in optionList.Elements ) {%>
     <div><input type="<%=selectType %>" answer="<%=question.Text("answer")%>" name="<%=question.Text("questionId")%>" value="<%=column.Name%>" id="<%=question.Text("questionId")%>_<%=column.Name %>"/>
      <label    labId="<%=question.Text("questionId")%>_<%=column.Name %>" for="<%=question.Text("questionId")%>_<%=column.Name %>"><%=column.Name %>、<%= optionList.Text(column.Name)%></label>
       </div>
    <%} %>
    </div>
          </div>
  <br/>  <br/>  <br/>
  <%} %>
  </div>
 
    <script language="javascript">
     

        $(function () {
            $('.nav li').click(function (e) {
                $('.nav li').removeClass('active');
                //$(e.target).addClass('active');
                $(this).addClass('active');
            });
        });


        function CheckAnswer() {
            var showAnswer = false;
            $("span[name=answer]").toggle();
           
            if ($("span[name=answer]").css('display')!= "none") {
                showAnswer = true;
            }
            if (showAnswer) {
                $("input:checked").each(function (obj) {
                    var value = $(this).val();
                    var answer = $(this).attr("answer");
                    //答案不符合
                    if (answer.indexOf(value) == -1) {
                        var id = $(this).attr("name") + "_" + $(this).val();
                        $("label[labId=" + id + "]").attr("style", "color:Red");
                        $("label[labId=" + id + "]").attr("isWrong", "true");
                    }
                    return;

                });
                var wrongAnswerCount = 0;
                var correctAnswerCount = 0;
                //遍历答案
                $("div[name='question']").each(function (obj) {
                    var questionId = $(this).attr("questionId");
                    var answer = $(this).attr("answer");
                    var selectAnswer = "";

                    $("input[name=" + questionId + "]:checked").each(function () {
                        selectAnswer += $(this).val();
                    })
                    if (selectAnswer != "") {
                        if (answer != selectAnswer) {
                            wrongAnswerCount++;
                            var id = "title_" + questionId;
                            $("#" + id).attr("style", "color:Red");
                            $("#" + id).attr("isWrong", "true");
                        } else {
                            correctAnswerCount++;
                        }
                    }  
                     

                });
                if (wrongAnswerCount > 0) {
                    alert("当前答错了" + wrongAnswerCount + "题！" + "答对了" + correctAnswerCount + "题！");
                } else {
                    alert("当前答对了" + correctAnswerCount + "题！");
                }

              
            } else {
               $("label[isWrong='true']").attr("style", "");
                $("label[isWrong='true']").attr("isWrong", "");
            }
        }
         
    </script>
 </asp:Content>