using DapperContext.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperContext.Repositories;

public abstract class Repository<T> : IRepository<T>
{
    protected readonly IDbContext _context;

    public Repository(IDbContext context)
    {
        _context = context;
    }

    protected abstract string MainTable { get; }

    /*
@"CREATE TABLE [{tempTable}](
[ClientID] [int] NOT NULL,
[WeightDataSourceID] [int] NULL,
[RoadVehicleModelBrandID] [int] NULL,
[SeaVehicleTypeID] [int] NULL,
[AirVehicleTypeID] [int] NULL,
[InlandWaterwaysVehicleTypeID] [int] NULL,
[EmissionStandardID] [int] NULL)"
     */
    protected abstract string CreateTempTableCommand { get; }


/*
@"--retrieve number of updated and new values before any update
select A.Count from (
select Count(*) [Count],1 Scenario from {Table} m
inner join {tempTable} t on t.ClientID = m.ClientID
union
select Count(*) [Count],2 Scenario from {tempTable} t
left join {Table} m on t.ClientID = m.ClientID
where m.ClientID is null
) A order by A.Scenario

--update old
update m 
set m.WeightDataSourceID = t.WeightDataSourceID, m.ShipperName = t.ShipperName, m.ConsigneeName = t.ConsigneeName, m.IsRefrigerated = t.IsRefrigerated, m.SmartWayCategoryID = t.SmartWayCategoryID, m.VehicleLoadFactor = t.VehicleLoadFactor, m.VehicleEmptyRunning = t.VehicleEmptyRunning,  m.OriginCountryID = t.OriginCountryID, m.DestinationCountryID = t.DestinationCountryID, 
m.OriginLocodeID = t.OriginLocodeID, m.OriginClientSiteID = t.OriginClientSiteID, m.OriginAirportSiteID = t.OriginAirportSiteID, m.OriginCity = t.OriginCity, m.OriginAddress = t.OriginAddress, m.OriginZipCode = t.OriginZipCode, m.OriginLatitude = t.OriginLatitude, m.OriginLongitude = t.OriginLongitude,
m.DestinationLocodeID = t.DestinationLocodeID, m.DestinationClientSiteID = t.DestinationClientSiteID, m.DestinationAirportSiteID = t.DestinationAirportSiteID, m.DestinationCity = t.DestinationCity, m.DestinationAddress = t.DestinationAddress, m.DestinationZipCode = t.DestinationZipCode, m.DestinationLatitude = t.DestinationLatitude, m.DestinationLongitude = t.DestinationLongitude,
m.DistanceDataSourceID = t.DIstanceDataSourceID, m.FuelConsumptionDataSourceID = t.FuelConsumptionDataSourceID, m.FuelTypeID = t.FuelTypeID, m.TripFuelConsumptionDataSourceID = t.TripFuelConsumptionDataSourceID, m.IsOwned = t.IsOwned, m.CarrierID = t.CarrierID, m.TransportModeID = t.TransportModeID,
m.RoadVehicleTypeID = t.RoadVehicleTypeID, m.SeaVehicleTypeID = t.SeaVehicleTypeID, m.AirVehicleTypeID = t.AirVehicleTypeID, m.InlandWaterwaysVehicleTypeID = t.InlandWaterwaysVehicleTypeID, m.EmissionStandardID = t.EmissionStandardID
from {Table} m
inner join {tempTable} t on t.ClientID = m.ClientID 

--add new
insert into {Table}(ClientID, WeightDataSourceID, ShipperName, ConsigneeName, IsRefrigerated, SmartWayCategoryID, VehicleLoadFactor, VehicleEmptyRunning, OriginCountryID, DestinationCountryID, OriginLocodeID, OriginClientSiteID, OriginAirportSiteID, OriginCity, OriginAddress, OriginZipCode, OriginLatitude, OriginLongitude, DestinationLocodeID, DestinationClientSiteID, DestinationAirportSiteID, DestinationCity, DestinationAddress, DestinationZipCode, DestinationLatitude, DestinationLongitude, DistanceDataSourceID, FuelConsumptionDataSourceID, FuelTypeID, TripFuelConsumptionDataSourceID, IsOwned, CarrierID, TransportModeID, RoadVehicleTypeID, RoadVehicleModelBrandID, SeaVehicleTypeID, AirVehicleTypeID, InlandWaterwaysVehicleTypeID, EmissionStandardID) 
select t.ClientID, t.WeightDataSourceID, t.ShipperName, t.ConsigneeName, t.IsRefrigerated, t.SmartWayCategoryID, t.VehicleLoadFactor, t.VehicleEmptyRunning, t.OriginCountryID, t.DestinationCountryID, t.OriginLocodeID, t.OriginClientSiteID, t.OriginAirportSiteID, t.OriginCity, t.OriginAddress, t.OriginZipCode, t.OriginLatitude, t.OriginLongitude, t.DestinationLocodeID, t.DestinationClientSiteID, t.DestinationAirportSiteID, t.DestinationCity, t.DestinationAddress, t.DestinationZipCode, t.DestinationLatitude, t.DestinationLongitude, t.DistanceDataSourceID, t.FuelConsumptionDataSourceID, t.FuelTypeID, t.TripFuelConsumptionDataSourceID, t.IsOwned, t.CarrierID, t.TransportModeID, t.RoadVehicleTypeID, t.RoadVehicleModelBrandID, t.SeaVehicleTypeID, t.AirVehicleTypeID, t.InlandWaterwaysVehicleTypeID, t.EmissionStandardID
from {tempTable} t
left join {Table} m on t.ClientID = m.ClientID
where m.ClientID is null"
 */
    protected abstract string AddAndUpdateSqlCommand { get; }


    /*
protected override Func<IEnumerable<Vehicle>, string, Task>? PostUpdateAction =>
        async (items,tempTable) =>
        {

            //we assume that a single client is per shipments import
            string sqlIds = $"select v.ID,v.ClientVehicleID from Vehicles v inner join {tempTable} t on v.ClientVehicleID = t.ClientVehicleID and v.ClientID = t.ClientID";
            var ids = (await _context.Query<(Guid ID, string ClientVehicleID)>(sqlIds)).ToDictionary(e => e.ClientVehicleID, e => e.ID);
            foreach (var v in items)
                v.Id = ids[v.ClientVehicleId];

        };
     */
    protected virtual Func<IEnumerable<T>, string, Task>? PostUpdateAction { get; }

    public async Task<(int Updated, int Added)> UpdateOldAndAddNew(List<T> items, bool updateIds)
        => await _context.UpdateOldAndAddNew(
            items,
            MainTable,
            CreateTempTableCommand,
            AddAndUpdateSqlCommand,
            updateIds ? PostUpdateAction : null);
}
