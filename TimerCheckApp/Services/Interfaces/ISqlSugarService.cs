using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace TimerCheckApp.Services.Interfaces
{
    /// <summary>
    /// SQLSugar服务接口
    /// </summary>
    public interface ISqlSugarService
    {
        /// <summary>
        /// 获取SQLSugar客户端实例
        /// </summary>
        /// <returns>SQLSugar客户端实例</returns>
        SqlSugarClient GetSqlSugarClient();

        /// <summary>
        /// 创建数据库
        /// </summary>
        void CreateDatabase();

        /// <summary>
        /// 创建表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        void CreateTable<T>() where T : class, new();

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>插入成功返回true，否则返回false</returns>
        bool Insert<T>(T entity) where T : class, new();

        /// <summary>
        /// 异步插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>插入成功返回true，否则返回false</returns>
        Task<bool> InsertAsync<T>(T entity) where T : class, new();

        /// <summary>
        /// 批量插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        /// <returns>插入成功返回true，否则返回false</returns>
        bool InsertRange<T>(List<T> entities) where T : class, new();

        /// <summary>
        /// 异步批量插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        /// <returns>插入成功返回true，否则返回false</returns>
        Task<bool> InsertRangeAsync<T>(List<T> entities) where T : class, new();

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>更新成功返回true，否则返回false</returns>
        bool Update<T>(T entity) where T : class, new();

        /// <summary>
        /// 异步更新数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>更新成功返回true，否则返回false</returns>
        Task<bool> UpdateAsync<T>(T entity) where T : class, new();

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>删除成功返回true，否则返回false</returns>
        bool Delete<T>(T entity) where T : class, new();

        /// <summary>
        /// 异步删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体</param>
        /// <returns>删除成功返回true，否则返回false</returns>
        Task<bool> DeleteAsync<T>(T entity) where T : class, new();

        /// <summary>
        /// 根据条件删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>删除成功返回true，否则返回false</returns>
        bool DeleteByCondition<T>(Expression<Func<T, bool>> whereExpression) where T : class, new();

        /// <summary>
        /// 异步根据条件删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>删除成功返回true，否则返回false</returns>
        Task<bool> DeleteByConditionAsync<T>(Expression<Func<T, bool>> whereExpression) where T : class, new();

        /// <summary>
        /// 查询单个实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>实体</returns>
        T GetSingle<T>(Expression<Func<T, bool>> whereExpression) where T : class, new();

        /// <summary>
        /// 异步查询单个实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>实体</returns>
        Task<T> GetSingleAsync<T>(Expression<Func<T, bool>> whereExpression) where T : class, new();

        /// <summary>
        /// 查询实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>实体列表</returns>
        List<T> GetList<T>(Expression<Func<T, bool>> whereExpression = null) where T : class, new();

        /// <summary>
        /// 异步查询实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="whereExpression">条件表达式</param>
        /// <returns>实体列表</returns>
        Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> whereExpression = null) where T : class, new();
    }
}