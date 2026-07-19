namespace DapperContext.Interfaces;

//public interface IRepository<T_Db> : IRepository<T_Db, T_Db> { }

//public interface IRepository<T_Db, T_View>
public interface IRepository<T_Db>
{
    Task<(int Updated, int Added)> UpdateOldAndAddNew(List<T_Db> items, bool updateIds);

    //Task<int> Delete(object? parameters);
    //Task<List<T_View>> Get(object? parameters);
}
