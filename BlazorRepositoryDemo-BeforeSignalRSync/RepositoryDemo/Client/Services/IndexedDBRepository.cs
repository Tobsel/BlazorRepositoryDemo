﻿using System.Reflection;

public class IndexedDBRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    // injected
    IBlazorDbFactory _dbFactory;
    string _dbName = "";
    string _primaryKeyName = "";
    bool _autoGenerateKey;

    IndexedDbManager manager;
    string storeName = "";
    Type entityType;
    PropertyInfo primaryKey;

    public IndexedDBRepository(string dbName, string primaryKeyName,
          bool autoGenerateKey, IBlazorDbFactory dbFactory)
    {
        _dbName = dbName;
        _dbFactory = dbFactory;
        _primaryKeyName = primaryKeyName;
        _autoGenerateKey = autoGenerateKey;

        entityType = typeof(TEntity);
        storeName = entityType.Name;
        primaryKey = entityType.GetProperty(primaryKeyName);
    }

    private async Task EnsureManager()
    {
        if (manager == null)
        {
            manager = await _dbFactory.GetDbManager(_dbName);
            await manager.OpenDb();
        }
    }

    public async Task DeleteAllAsync()
    {
        await EnsureManager();
        await manager.ClearTableAsync(storeName);
    }

    public async Task<bool> DeleteAsync(TEntity EntityToDelete)
    {
        await EnsureManager();
        var Id = primaryKey.GetValue(EntityToDelete);
        return await DeleteByIdAsync(Id);
    }

    public async Task<bool> DeleteByIdAsync(object Id)
    {
        await EnsureManager();
        try
        {
            await manager.DeleteRecordAsync(storeName, Id);
            return true;
        }
        catch (Exception ex)
        {
            // log exception
            return false;
        }
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        await EnsureManager();
        var array = await manager.ToArray<TEntity>(storeName);
        if (array == null)
            return new List<TEntity>();
        else
            return array.ToList();
    }

    public async Task<IEnumerable<TEntity>> GetAsync(QueryFilter<TEntity> Filter)
    {
        // We have to load all items and use LINQ to filter them. :(
        var allitems = await GetAllAsync();
        return Filter.GetFilteredList(allitems);
    }

    public async Task<TEntity> GetByIdAsync(object Id)
    {
        await EnsureManager();
        var items = await manager.Where<TEntity>(storeName, _primaryKeyName, Id);
        if (items.Any())
            return items.First();
        else
            return null;
    }

    public async Task<TEntity> InsertAsync(TEntity Entity)
    {
        await EnsureManager();

        try
        {
            var record = new StoreRecord<TEntity>()
            {
                StoreName = storeName,
                Record = Entity
            };
            await manager.AddRecordAsync<TEntity>(record);
            var allItems = await GetAllAsync();
            var last = allItems.Last();
            return last;
        }
        catch (Exception ex)
        {
            // log exception
            return null;
        }
    }

    public async Task<TEntity> UpdateAsync(TEntity EntityToUpdate)
    {
        await EnsureManager();
        object Id = primaryKey.GetValue(EntityToUpdate);
        try
        {
            await manager.UpdateRecord(new UpdateRecord<TEntity>()
            {
                StoreName = storeName,
                Record = EntityToUpdate,
                Key = Id
            });
            return EntityToUpdate;
        }
        catch (Exception ex)
        {
            // log exception
            return null;
        }
    }
}