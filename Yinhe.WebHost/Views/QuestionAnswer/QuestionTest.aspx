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
        <h1><strong>太太随机模拟考试题</strong><small><a href="/QuestionAnswer/Index">返回</a></small></h1>
          <small><a href="javascript:;" onclick="window.location.reload();">重新测试</a></small> 
       <button class="btn btn-lg btn-primary navbar-fixed-top" type="button" id="answerBtn" onclick='CheckAnswer()'>查看/隐藏答案</button>
     </div>
        <h1 class="navbar-fixed-top" id="secondText"></h1>
<%
    string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
    DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
    var pointLibIds= dataOp.FindAllByQuery("WPM_QuestionLibrary", Query.In("type", "重点把握类")).Select(c=>(BsonValue)c.Text("libId"));
    var allQuestionList = dataOp.FindAllByQuery("WPM_Question", Query.In("libId", pointLibIds)).ToList();
    var rand=new Random();
    var singelQuestionList = allQuestionList.Where(c => c.Text("group") == "Singel").OrderBy(c => rand.Next(10000));
    var mutipleQuestionList = allQuestionList.Where(c => c.Text("group") == "Mutiple").OrderBy(c => rand.Next(10000));
    var yesOrNOQuestionList = allQuestionList.Where(c => c.Text("group") == "YesOrNO").OrderBy(c => rand.Next(10000));
    var instanceQuestionList = allQuestionList.Where(c => c.Text("group") == "Instance").OrderBy(c => rand.Next(10000));

    var hitAllList = new List<BsonDocument>();
    if (singelQuestionList.Count()>0)
    hitAllList.AddRange(singelQuestionList.Take(singelQuestionList.Count() / 2));
    if (mutipleQuestionList.Count() > 0)
    hitAllList.AddRange(mutipleQuestionList.Take(mutipleQuestionList.Count() / 2));
    if (yesOrNOQuestionList.Count() > 0)
    hitAllList.AddRange(yesOrNOQuestionList.Take(yesOrNOQuestionList.Count() / 2));
    if (instanceQuestionList.Count() > 0)
    hitAllList.AddRange(instanceQuestionList.Take(instanceQuestionList.Count() / 2));

    var questionCount = PageReq.GetParamInt("questionCount");
    if (questionCount > 0)
    {
        hitAllList = hitAllList.OrderBy(c => rand.Next(10000)).Take(questionCount).ToList();
    }
    var quickMode = PageReq.GetParamInt("quickMode");//快速答题模式;
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

  foreach (var question in hitAllList)
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
        <div>
      <input type="<%=selectType %>" answer="<%=question.Text("answer")%>" name="<%=question.Text("questionId")%>" value="<%=column.Name%>" id="<%=question.Text("questionId")%>_<%=column.Name %>"/>
      <label    labId="<%=question.Text("questionId")%>_<%=column.Name %>" for="<%=question.Text("questionId")%>_<%=column.Name %>"><%=column.Name %>、<%= optionList.Text(column.Name)%></label>
   </div>
    <%} %>
    </div>
          </div>
  <br/>  <br/>  <br/>
  <%} %>
  </div>
 
    <script language="javascript">
     var startSecond=10;
     var intervalId;
     var allQuestionCount=<%=hitAllList.Count %>;
   
        $(function () {
            $('.nav li').click(function (e) {
                $('.nav li').removeClass('active');
                //$(e.target).addClass('active');
                $(this).addClass('active');
              
               
            });
        });
        
        <%if(quickMode==1){ %>
        intervalId=window.setInterval(CheckResult,1000);
        <%} %>
        function CheckResult()
        {
     
          if(startSecond<0)
          {
           
            CheckAnswer();

          }else
          {
              $("#secondText").html(startSecond);
              startSecond-=1;
          }
         
        }

       

        function CheckAnswer() {
           if( typeof(intervalId)!="undefined")
             {
                window.clearInterval(intervalId);
             }

            var showAnswer = false;
            $("span[name=answer]").toggle();

            if ($("span[name=answer]").css('display') != "none") {
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

                var unAnswerCount=allQuestionCount-correctAnswerCount-wrongAnswerCount;
                 var message= "";
                if (wrongAnswerCount > 0) {
                    message+= "当前答错了" + wrongAnswerCount + "题！ ";
                } 
                 if (correctAnswerCount > 0) {
                    message+= " 答对了" + correctAnswerCount + "题！";
                } 
                 if (unAnswerCount > 0) {
                    message+= "剩余" + unAnswerCount + "题未答！";
                } 
                
               
                 if(confirm(message+"是否继续新的挑战"))
                 {
                   window.location.reload();
                 }else
                 {
                   $("answerBtn").hide();
                  // window.location.reload();
                 }
              

            } else {
                $("label[isWrong='true']").attr("style", "");
                $("label[isWrong='true']").attr("isWrong", "");
            }
        }
         
    </script>
 </asp:Content>