using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 枚举相关
    /// </summary>
    public class EnumCollection
    {
    }

    /// <summary>
    /// 分项专业的类型
    /// </summary>
    public enum ProjectProfessionType
    {
        /// <summary>
        /// 辅助专业
        /// </summary>
        [EnumDescription("辅助专业")]
        Minor = 0,
        /// <summary>
        /// 主专业
        /// </summary>
        [EnumDescription("主专业")]
        Main = 1
    }

    /// <summary>
    /// 分项计划的状态
    /// </summary>
    public enum ProjectPlanStatus
    {
        /// <summary>
        /// 未确认
        /// </summary>
        [EnumDescription("未确认")]
        Unconfirmed = 0,
        /// <summary>
        /// 进行中
        /// </summary>
        [EnumDescription("进行中")]
        Processing = 1,
        /// <summary>
        /// 已完成
        /// </summary>
        [EnumDescription("已完成")]
        Completed = 2
    }

    /// <summary>
    /// 角色类型,主要用于ProjRoleManager表
    /// </summary>
    public enum ProjRoleType
    {
        /// <summary>
        /// 负责计划者
        /// </summary>
        [EnumDescription("负责计划者")]
        PlanOwner = 0,
        /// <summary>
        /// 分解计划者
        /// </summary>
        [EnumDescription("分解计划者")]
        PlanSpliter = 1,
        /// <summary>
        /// 负责任务者
        /// </summary>
        [EnumDescription("负责任务者")]
        TaskOwner = 2,
        /// <summary>
        /// 操作任务者
        /// </summary>
        [EnumDescription("操作任务者")]
        TaskOperator = 3,
        /// <summary>
        /// 参与任务者
        /// </summary>
        [EnumDescription("参与任务者")]
        TaskJoiner = 4,
        /// <summary>
        /// 检查工作者
        /// </summary>
        [EnumDescription("检查工作者")]
        TodoChecker = 5
    }

    /// <summary>
    /// 任务负责人类型,主要用于任务负责人表
    /// </summary>
    public enum TaskManagerType
    {
        /// <summary>
        /// 分解计划者
        /// </summary>
        [EnumDescription("分解计划者")]
        PlanSpliter = 0,
        /// <summary>
        /// 负责任务者
        /// </summary>
        [EnumDescription("负责任务者")]
        TaskOwner = 1,
        /// <summary>
        /// 操作任务者
        /// </summary>
        [EnumDescription("操作任务者")]
        TaskOperator = 2,
        /// <summary>
        /// 参与任务者
        /// </summary>
        [EnumDescription("参与任务者")]
        TaskJoiner = 3,
    }

    /// <summary>
    /// 任务状态
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// 待分解
        /// </summary>
        [EnumDescription("待分解")]
        ToSplit = 0,
        /// <summary>
        /// 分解完成
        /// </summary>
        [EnumDescription("分解完成")]
        SplitCompleted = 1,
        /// <summary>
        /// 未开始
        /// </summary>
        [EnumDescription("未开始")]
        NotStarted = 2,
        /// <summary>
        /// 进行中
        /// </summary>
        [EnumDescription("进行中")]
        Processing = 3,
        /// <summary>
        /// 已完成
        /// </summary>
        [EnumDescription("已完成")]
        Completed = 4
    }
    /// <summary>
    /// 月计划任务节点的类型
    /// </summary>
    public enum TaskNodeType
    {
        /// <summary>
        /// 自定义任务
        /// </summary>
        [EnumDescription("自定义任务")]
        SelfDefinedTask = 0,
        /// <summary>
        /// 项目计划任务
        /// </summary>
        [EnumDescription("项目计划任务")]
        DesignTask = 1,
        /// <summary>
        /// 系统领导任务
        /// </summary>
        [EnumDescription("系统领导任务")]
        SystemTask = 2,
        /// <summary>
        /// 上月度未完成任务
        /// </summary>
        [EnumDescription("上月度未完成任务")]
        LastMonthUnfinishedTask = 3
    }
    /// <summary>
    /// 月计划中任务延迟状态
    /// </summary>
    public enum TaskDelayStatus
    {
        /// <summary>
        /// 未延迟
        /// </summary>
        [EnumDescription("未延迟")]
        NotDelayed = 0,
        /// <summary>
        /// 项目计划任务
        /// </summary>
        [EnumDescription("已延迟")]
        Delayed = 1
    }

    /// <summary>
    /// 有计划中任务是否有效状态
    /// </summary>
    public enum TaskValidStatus
    {
        /// <summary>
        /// 有效
        /// </summary>
        [EnumDescription("任务有效")]
        Valid = 0,
        /// <summary>
        /// 无效
        /// </summary>
        [EnumDescription("任务无效")]
        Invalid = 1
    }

    /// <summary>
    /// 决策点类型
    /// </summary>
    public enum DecisionPointType
    {
        /// <summary>
        /// 圆
        /// </summary>
        [EnumDescription("圆")]
        Circle = 0,
        /// <summary>
        /// 矩形
        /// </summary>
        [EnumDescription("矩形")]
        Rect = 1,
        /// <summary>
        /// 圆角矩形
        /// </summary>
        [EnumDescription("圆角矩形")]
        RoundRect = 2
    }

    /// <summary>
    /// 决策点状态
    /// </summary>
    public enum DecisionPointStatus
    {
        /// <summary>
        /// 无状态，未挂接任何任务
        /// </summary>
        NotStatus = 0,
        /// <summary>
        /// 未开始
        /// </summary>
        [EnumDescription("未开始")]
        NotStarted = 1,
        /// <summary>
        /// 进行中
        /// </summary>
        [EnumDescription("进行中")]
        Processing = 2,
        /// <summary>
        /// 已完成
        /// </summary>
        [EnumDescription("已完成")]
        Completed = 3,
        /// <summary>
        /// 已延迟
        /// </summary>
        [EnumDescription("已延迟")]
        Delayed = 4
    }
    /// <summary>
    /// 任务节点类型
    /// </summary>
    public enum TaskType
    {
        /// <summary>
        /// 外立面
        /// </summary>
        [EnumDescription("外立面")]
        Facade = 1,

        /// <summary>
        /// 景观
        /// </summary>
        [EnumDescription("景观")]
        Landscape = 2,

        /// <summary>
        /// 精装修
        /// </summary>
        [EnumDescription("精装修")]
        Decoration = 5,

        /// <summary>
        /// 设备技术
        /// </summary>
        [EnumDescription("设备技术")]
        Equipment = 6,

        /// <summary>
        /// 产品初步策划
        /// </summary>
        [EnumDescription("产品初步策划")]
        ProductPlan = 100,

        /// <summary>
        /// 成本配置
        /// </summary>
        [EnumDescription("成本配置")]
        CostConfig = 101
    }


    /// <summary>
    /// 任务业务节点类型
    /// </summary>
    public enum ConcernNodeType
    {
        /// <summary>
        /// 费用支付节点
        /// </summary>
        [EnumDescription("费用支付")]
        FeePayment = 1,

        /// <summary>
        /// 合同节点
        /// </summary>
        [EnumDescription("合同节点")]
        ContractNode = 2,

        /// <summary>
        /// 供应商
        /// </summary>
        [EnumDescription("供应商")]
        Supplier = 3,

        /// <summary>
        /// 任务书
        /// </summary>
        [EnumDescription("任务书")]
        TaskPage = 4,
        /// <summary>
        /// 一二级联动
        /// </summary>
        [EnumDescription("一二级联动")]
        LevelRelate = 5,

        /// <summary>
        /// 阶段成果
        /// </summary>
        [EnumDescription("阶段成果")]
        StageResult = 6,

        /// <summary>
        /// 多审批任务
        /// </summary>
        [EnumDescription("多审批任务")]
        MultiApproval=7,

        /// <summary>
        /// 开工节点
        /// </summary>
        [EnumDescription("开工节点")]
        StartWorkTask = 8,

        /// <summary>
        /// 开放节点
        /// </summary>
        [EnumDescription("开放节点")]
        OpenTask = 9
    }

    /// <summary>
    /// 金地任务
    /// </summary>
    public enum ExemptSkipType
    {
        /// <summary>
        /// 外立面
        /// </summary>
        [EnumDescription("待审批")]
        Verify = 0,

        /// <summary>
        /// 景观
        /// </summary>
        [EnumDescription("通过")]
        Passed = 1,

        /// <summary>
        /// 精装修
        /// </summary>
        [EnumDescription("拒绝")]
        Refused = 2,


    }
    /// <summary>
    ///  脉络图阶段
    /// </summary>
    public enum StageType
    {
        /// <summary>
        /// 投资决策阶段
        /// </summary>
        [EnumDescription("投资决策阶段")]
        Investment = 57,

        /// <summary>
        /// 产品策划及方案设计阶段
        /// </summary>
        [EnumDescription("产品策划及方案设计阶段")]
        ProductPlan = 58,

        /// <summary>
        /// 扩初设计及施工图阶段
        /// </summary>
        [EnumDescription("扩初设计及施工图阶段")]
        ExtendedDesign = 59,

        /// <summary>
        /// 工程建造阶段
        /// </summary>
        [EnumDescription("工程建造阶段")]
        Construct = 60

    }

    /// <summary>
    /// 流程动作类型 发起 会签 审核 批准
    /// </summary>
    public enum FlowActionType
    {
        /// <summary>
        /// 发起
        /// </summary>
        [EnumDescription("发起")]
        Launch = 1,

        /// <summary>
        /// 会签
        /// </summary>
        [EnumDescription("会签")]
        Countersign = 2,

        /// <summary>
        /// 审核
        /// </summary>
        [EnumDescription("审核")]
        Audit = 3,

        /// <summary>
        /// 批准
        /// </summary>
        [EnumDescription("批准")]
        Approve = 4
    }

    /// <summary>
    /// 流程动作类型 发起 会签 审核 批准
    /// </summary>
    public enum FlowActionExecType
    {
        /// <summary>
        /// 发起
        /// </summary>
        [EnumDescription("发起流程")]
        Launch = 0,

        /// <summary>
        /// 会签确认
        /// </summary>
        [EnumDescription("会签确认")]
        Countersign = 1,

        /// <summary>
        /// 通过
        /// </summary>
        [EnumDescription("通过")]
        Approve = 2,

        /// <summary>
        /// 驳回
        /// </summary>
        [EnumDescription("驳回")]
        Refuse = 3,
           /// <summary>
        /// 驳回
        /// </summary>
        [EnumDescription("集团驳回")]
        GroupRefuse = 4
    }

    /// <summary>
    /// 供应商审批状态枚举
    /// </summary>
    public enum SupplierType
    {
        /// <summary>
        ///入库待审
        /// </summary>
        [EnumDescription("入库待审")]
        Verify = 0,

        /// <summary>
        /// 会签
        /// </summary>
        [EnumDescription("审批中")]
        accept = 1,

        /// <summary>
        /// 审核
        /// </summary>
        [EnumDescription("审批通过")]
        Refused = 2,


    }

    /// <summary>
    /// 系统日志操作类型
    /// </summary>
    public enum SysLogType
    {
        /// <summary>
        /// 普通操作
        /// </summary>
        [EnumDescription("普通操作")]
        General = 0,

        /// <summary>
        /// 登陆
        /// </summary>
        [EnumDescription("登陆")]
        Login = 1,

        /// <summary>
        /// 登出
        /// </summary>
        [EnumDescription("登出")]
        Logout = 2,

        /// <summary>
        /// 新增
        /// </summary>
        [EnumDescription("新增")]
        Insert = 3,

        /// <summary>
        /// 编辑
        /// </summary>
        [EnumDescription("编辑")]
        Update = 4,

        /// <summary>
        /// 删除
        /// </summary>
        [EnumDescription("删除")]
        Delete = 5
    }


    /// <summary>
    /// 月度计划任务状态
    /// </summary>
    public enum MonthlyTaskStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        [EnumDescription("未开始")]
        NotStarted = 0,
        /// <summary>
        /// 已完成
        /// </summary>
        [EnumDescription("已完成")]
        Completed = 1,
        /// <summary>
        /// 延期
        /// </summary>
        [EnumDescription("延期")]
        Delayed = 2
    }

    /// <summary>
    /// 月度计划任务状态
    /// </summary>
    public enum SysTaskStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        [EnumDescription("未开始")]
        NotStarted = 0,
        /// <summary>
        /// 已指派
        /// </summary>
        [EnumDescription("已指派")]
        Processing = 1,
        /// <summary>
        /// 已反馈
        /// </summary>
        [EnumDescription("已反馈")]
        Feedbacked = 2,
        /// <summary>
        ///已完成
        /// </summary>
        [EnumDescription("已完成")]
        Completed = 3
    }
    /// <summary>
    /// 月计划状态
    /// </summary>
    public enum MonthlyPlanStatus
    {
        /// <summary>
        /// 未发布
        /// </summary>
        [EnumDescription("未发布")]
        NotPublished = 0,
        /// <summary>
        /// 已发布
        /// </summary>
        [EnumDescription("已发布")]
        Published = 1,
        /// <summary>
        /// 已归档
        /// </summary>
        [EnumDescription("已归档")]
        Filed = 2
    }
    /// <summary>
    /// 通用任务状态枚举
    /// </summary>
    public enum CommonTaskStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        [EnumDescription("未开始")]
        NotStarted = 0,
        /// <summary>
        /// 进行中
        /// </summary>
        [EnumDescription("进行中")]
        Processing = 1,
        /// <summary>
        ///已完成
        /// </summary>
        [EnumDescription("已完成")]
        Completed = 2,
        /// <summary>
        ///延期
        /// </summary>
        [EnumDescription("延期")]
        Delayed = 3,
        /// <summary>
        /// 待分解
        /// </summary>
        [EnumDescription("待分解")]
        ToSplit = 4,
        /// <summary>
        /// 分解完成
        /// </summary>
        [EnumDescription("分解完成")]
        SplitCompleted = 5,
        /// <summary>
        /// 已反馈
        /// </summary>
        [EnumDescription("已反馈")]
        Feedbacked = 6
    }

    /// <summary>
    /// 产品系列客群特征类型
    /// </summary>
    public enum SegmentFeatureType
    {
        /// <summary>
        /// 客户年龄
        /// </summary>
        [EnumDescription("客户年龄")]
        Age = 1,
        /// <summary>
        /// 客户区域
        /// </summary>
        [EnumDescription("客户区域")]
        Area = 2,
        /// <summary>
        /// 旧客户
        /// </summary>
        [EnumDescription("旧客户")]
        OldClient = 3,
        /// <summary>
        /// 付款方式
        /// </summary>
        [EnumDescription("付款方式")]
        PayType = 4,
        /// <summary>
        /// 按揭年限
        /// </summary>
        [EnumDescription("按揭年限")]
        YearCount = 5,
        /// <summary>
        /// 按揭额
        /// </summary>
        [EnumDescription("按揭额")]
        LoanAmount = 6,
    }

    #region 设计变更指令单状态
    /// <summary>
    /// 设计变更指令单状态
    /// </summary>
    public enum DesignChangeCmdStatus
    {
        /// <summary>
        /// 发起人未提交
        /// </summary>
        [EnumDescription("未提交")]
        NotLaunch = 0,
        /// <summary>
        /// 未签发(签发人未选择签收人进行签发)
        /// </summary>
        [EnumDescription("未签发")]
        NotIssue = 1,
        /// <summary>
        /// 未阅(签收人未阅读)
        /// </summary>
        [EnumDescription("未阅")]
        NotSee = 2,
        /// <summary>
        /// 已阅(所有?签收人已阅读)
        /// </summary>
        [EnumDescription("已阅")]
        Seen = 3
    }
    #endregion
    
}
