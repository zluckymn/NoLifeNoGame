﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.18052
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Yinhe.WebHost.ServiceReference1 {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="ServiceReference1.TodoListSoap")]
    public interface TodoListSoap {
        
        // CODEGEN: 命名空间 http://tempuri.org/ 的元素名称 loginName 以后生成的消息协定未标记为 nillable
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/GetTodoList", ReplyAction="*")]
        Yinhe.WebHost.ServiceReference1.GetTodoListResponse GetTodoList(Yinhe.WebHost.ServiceReference1.GetTodoListRequest request);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class GetTodoListRequest {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Name="GetTodoList", Namespace="http://tempuri.org/", Order=0)]
        public Yinhe.WebHost.ServiceReference1.GetTodoListRequestBody Body;
        
        public GetTodoListRequest() {
        }
        
        public GetTodoListRequest(Yinhe.WebHost.ServiceReference1.GetTodoListRequestBody Body) {
            this.Body = Body;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.Runtime.Serialization.DataContractAttribute(Namespace="http://tempuri.org/")]
    public partial class GetTodoListRequestBody {
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=0)]
        public string loginName;
        
        [System.Runtime.Serialization.DataMemberAttribute(Order=1)]
        public int pageSize;
        
        [System.Runtime.Serialization.DataMemberAttribute(Order=2)]
        public int pageIndex;
        
        public GetTodoListRequestBody() {
        }
        
        public GetTodoListRequestBody(string loginName, int pageSize, int pageIndex) {
            this.loginName = loginName;
            this.pageSize = pageSize;
            this.pageIndex = pageIndex;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class GetTodoListResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Name="GetTodoListResponse", Namespace="http://tempuri.org/", Order=0)]
        public Yinhe.WebHost.ServiceReference1.GetTodoListResponseBody Body;
        
        public GetTodoListResponse() {
        }
        
        public GetTodoListResponse(Yinhe.WebHost.ServiceReference1.GetTodoListResponseBody Body) {
            this.Body = Body;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.Runtime.Serialization.DataContractAttribute(Namespace="http://tempuri.org/")]
    public partial class GetTodoListResponseBody {
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=0)]
        public string GetTodoListResult;
        
        public GetTodoListResponseBody() {
        }
        
        public GetTodoListResponseBody(string GetTodoListResult) {
            this.GetTodoListResult = GetTodoListResult;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface TodoListSoapChannel : Yinhe.WebHost.ServiceReference1.TodoListSoap, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class TodoListSoapClient : System.ServiceModel.ClientBase<Yinhe.WebHost.ServiceReference1.TodoListSoap>, Yinhe.WebHost.ServiceReference1.TodoListSoap {
        
        public TodoListSoapClient() {
        }
        
        public TodoListSoapClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public TodoListSoapClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public TodoListSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public TodoListSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Yinhe.WebHost.ServiceReference1.GetTodoListResponse Yinhe.WebHost.ServiceReference1.TodoListSoap.GetTodoList(Yinhe.WebHost.ServiceReference1.GetTodoListRequest request) {
            return base.Channel.GetTodoList(request);
        }
        
        public string GetTodoList(string loginName, int pageSize, int pageIndex) {
            Yinhe.WebHost.ServiceReference1.GetTodoListRequest inValue = new Yinhe.WebHost.ServiceReference1.GetTodoListRequest();
            inValue.Body = new Yinhe.WebHost.ServiceReference1.GetTodoListRequestBody();
            inValue.Body.loginName = loginName;
            inValue.Body.pageSize = pageSize;
            inValue.Body.pageIndex = pageIndex;
            Yinhe.WebHost.ServiceReference1.GetTodoListResponse retVal = ((Yinhe.WebHost.ServiceReference1.TodoListSoap)(this)).GetTodoList(inValue);
            return retVal.Body.GetTodoListResult;
        }
    }
}
