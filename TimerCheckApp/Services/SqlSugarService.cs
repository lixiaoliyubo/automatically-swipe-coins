using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Services
{
    /// <summary>
    /// SQLSugar服务实现类
    /// </summary>
    public class SqlSugarService : ISqlSugarService
    {
        private readonly ILogService _logService;
        private readonly string _connectionString;
        private readonly SqlSugarClient _sqlSugarClient;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="dbName">数据库名称</param>
        public SqlSugarService(ILogService logService, string dbName = "AdAuto.db")
        {
            _logService = logService;
            
            // 确保数据库目录存在
            var dbDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }
            
            var dbPath = Path.Combine(dbDirectory, dbName);
            _connectionString = $"DataSource={dbPath}";
            
            _sqlSugarClient = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = _connectionString,
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            
            // 配置日志
            _sqlSugarClient.Aop.OnLogExecuting = (sql, parameters) =>
            {
                _logService.LogInfo($"SQL: {sql}", "SqlSugarService");
            };
            
            _logService.LogInfo("SQLite数据库服务已初始化", "SqlSugarService");
        }

        /// <summary>
        /// 获取SQLSugar客户端实例
        /// </summary>
        /// <returns>SQLSugar客户端实例</returns>
        public SqlSugarClient GetSqlSugarClient()
        {
            return _sqlSugarClient;
        }

        /// <summary>
        /// 创建数据库
        /// </summary>
        public void CreateDatabase()
        {
            // SQLite不需要显式创建数据库，连接时会自动创建
            _logService.LogInfo("SQLite数据库已准备就绪", "SqlSugarService");
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        public void CreateTable<T>() where T : class, new()
        {
            try
            {
                _sqlSugarClient.CodeFirst.InitTables<T>();
                _logService.LogInfo($"表 {typeof(T).Name} 已创建或已存在", "SqlSugarService");
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建表 {typeof(T).Name} 失败: {ex.Message}", "SqlSugarService");
                throw;
            }
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>插入成功返回true，否则返回false</returns>
        public bool Insert<T>(T entity) where T : class, new()
        {
            try
            {
                return _sqlSugarClient.Insertable(entity).ExecuteCommand() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"插入数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 异步插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>插入成功返回true，否则返回false</returns>
        public async Task<bool> InsertAsync<T>(T entity) where T : class, new()
        {
            try
            {
                return await _sqlSugarClient.Insertable(entity).ExecuteCommandAsync() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步插入数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 批量插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        /// <returns>插入成功返回true，否则返回false</returns>
        public bool InsertRange<T>(List<T> entities) where T : class, new()
        {
            try
            {
                return _sqlSugarClient.Insertable(entities).ExecuteCommand() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"批量插入数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 异步批量插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        /// <returns>插入成功返回true，否则返回false</returns>
        public async Task<bool> InsertRangeAsync<T>(List<T> entities) where T : class, new()
        {
            try
            {
                return await _sqlSugarClient.Insertable(entities).ExecuteCommandAsync() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步批量插入数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>更新成功返回true，否则返回false</returns>
        public bool Update<T>(T entity) where T : class, new()
        {
            try
            {
                return _sqlSugarClient.Updateable(entity).ExecuteCommand() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"更新数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 异步更新数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>更新成功返回true，否则返回false</returns>
        public async Task<bool> UpdateAsync<T>(T entity) where T : class, new()
        {
            try
            {
                return await _sqlSugarClient.Updateable(entity).ExecuteCommandAsync() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步更新数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>删除成功返回true，否则返回false</returns>
        public bool Delete<T>(T entity) where T : class, new()
        {
            try
            {
                return _sqlSugarClient.Deleteable<T>().In(entity).ExecuteCommand() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"删除数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 异步删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>删除成功返回true，否则返回false</returns>
        public async Task<bool> DeleteAsync<T>(T entity) where T : class, new()
        {
            try
            {
                return await _sqlSugarClient.Deleteable<T>().In(entity).ExecuteCommandAsync() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步删除数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 根据条件删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>删除成功返回true，否则返回false</returns>
        public bool DeleteByCondition<T>(Expression<Func<T, bool>> whereExpression) where T : class, new()
        {
            try
            {
                return _sqlSugarClient.Deleteable<T>().Where(whereExpression).ExecuteCommand() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"根据条件删除数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 异步根据条件删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>删除成功返回true，否则返回false</returns>
        public async Task<bool> DeleteByConditionAsync<T>(Expression<Func<T, bool>> whereExpression) where T : class, new()
        {
            try
            {
                return await _sqlSugarClient.Deleteable<T>().Where(whereExpression).ExecuteCommandAsync() > 0;
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步根据条件删除数据失败: {ex.Message}", "SqlSugarService");
                return false;
            }
        }

        /// <summary>
        /// 查询单个实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>实体</returns>
        public T GetSingle<T>(Expression<Func<T, bool>> whereExpression) where T : class, new()
        {
            try
            {
                return _sqlSugarClient.Queryable<T>().First(whereExpression);
            }
            catch (Exception ex)
            {
                _logService.LogError($"查询单个实体失败: {ex.Message}", "SqlSugarService");
                return null;
            }
        }

        /// <summary>
        /// 异步查询单个实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>实体</returns>
        public async Task<T> GetSingleAsync<T>(Expression<Func<T, bool>> whereExpression) where T : class, new()
        {
            try
            {
                return await _sqlSugarClient.Queryable<T>().FirstAsync(whereExpression);
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步查询单个实体失败: {ex.Message}", "SqlSugarService");
                return null;
            }
        }

        /// <summary>
        /// 查询实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>实体列表</returns>
        public List<T> GetList<T>(Expression<Func<T, bool>> whereExpression = null) where T : class, new()
        {
            try
            {
                var query = _sqlSugarClient.Queryable<T>();
                if (whereExpression != null)
                {
                    query = query.Where(whereExpression);
                }
                return query.ToList();
            }
            catch (Exception ex)
            {
                _logService.LogError($"查询实体列表失败: {ex.Message}", "SqlSugarService");
                return new List<T>();
            }
        }

        /// <summary>
        /// 异步查询实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>实体列表</returns>
        public async Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> whereExpression = null) where T : class, new()
        {
            try
            {
                var query = _sqlSugarClient.Queryable<T>();
                if (whereExpression != null)
                {
                    query = query.Where(whereExpression);
                }
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logService.LogError($"异步查询实体列表失败: {ex.Message}", "SqlSugarService");
                return new List<T>();
            }
        }
    }
}