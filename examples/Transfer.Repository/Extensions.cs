using System;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Transfer.Repository
{
    public static class Extensions
    {
        public static void PGExceptionHandler<T>(this Exception e, ILogger<T> logger = null)
        {
            if (e is PostgresException postgresException)
            {
                switch (postgresException.SqlState)
                {
                    //忽略主键重复
                    case "23505":
                        logger?.LogWarning($"23505:忽略主键重复错误_{postgresException.Detail}");
                        return;
                    //忽略外键约束
                    case "23503":
                        logger?.LogWarning($"23503:忽略外键约束错误_{postgresException.Detail}");
                        return;
                    case "22021":
                        logger?.LogWarning($"22021:字符集错误invalid byte sequence for encoding UTF8: 0x00_{postgresException.Detail}");
                        return;
                    default:
                        logger?.LogError($"未知错误:{postgresException.Detail}", e);
                        throw e;
                }
            }

            if (e.InnerException != null)
            {
                PGExceptionHandler(e.InnerException, logger);
            }
            else
            {
                logger?.LogError($"error:{e.Message}", e);
                throw e;
            }
        }
    }
}
