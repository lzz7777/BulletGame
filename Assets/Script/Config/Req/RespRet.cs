namespace Apifox
{
    #region 基础接受结构

    public class RespRetBase
    {
        public static readonly RespRetBase Success = new()
        {
            code = 0,
            message = "本地跳过"
        };

        public static readonly RespRetBase Error = new()
        {
            code = -1,
            message = "未知错误"
        };

        public int code;
        public string message;

        public bool IsSuccess => code == 0;
    }

    public class RespRetString : RespRet<string>
    {
        public new static readonly RespRetString Success = new()
        {
            code = 0,
            message = "本地跳过"
        };

        public new static readonly RespRetString Error = new()
        {
            code = -1,
            message = "未知错误"
        };
    }

    public class RespRet<T> : RespRetBase
    {
        public new static readonly RespRet<T> Success = new()
        {
            code = 0,
            message = "本地跳过"
        };

        public new static readonly RespRet<T> Error = new()
        {
            code = -1,
            message = "未知错误"
        };

        public T data;
    }


    public class RespRetLst<T> : RespRetBase
    {
        public new static readonly RespRetLst<T> Success = new()
        {
            code = 0,
            message = "本地跳过"
        };

        public new static readonly RespRetLst<T> Error = new()
        {
            code = -1,
            message = "未知错误"
        };

        public T[] data;
    }

    #endregion

    public class StatusData
    {
        public int status;
    }

    public class gifts
    {
        //礼物图标
        public string gift_icon;

        //礼物ID
        public string gift_id;

        //礼物名称
        public string gift_name;

        //礼物单价
        public string gift_unit_price;
    }

    /// <summary>
    /// 积分达标的玩家
    /// </summary>
    public class PlayerBuffInfo
    {
        /// <summary>
        /// 当前分数
        /// </summary>
        public long currentScore;

        /// <summary>
        /// 玩家ID
        /// </summary>
        public string playerId;

        /// <summary>
        /// 分数阶段
        /// </summary>
        public long score;

        /// <summary>
        /// buff生效时间
        /// </summary>
        public long startTime;

        /// <summary>
        /// 结束时间
        /// </summary>
        public long endTime;
    }

    public class GiftsInfo
    {
        public gifts[] gifts;
    }

    public class RespGetStatus : RespRet<StatusData>
    {
    }

    public class RespGetSudConfig : RespRet<GiftsInfo>
    {
    }
}