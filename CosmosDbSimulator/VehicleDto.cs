using System.Collections.Generic;

namespace CosmosDbUtil
{
    public class VehicleResponseDto
    {
        public List<VehicleDto> Vehicles { get; set; }
        public double RUs { get; set; }
    }

    public class VehicleDto
    {
        public string id { get; set; }
        public string timestamp { get; set; }
        public string vin { get; set; }
        public string vehicleId { get; set; }
        public string customerId { get; set; }
        public string licensePlate { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string fuelType { get; set; }
        public string color { get; set; }
        public string milage { get; set; }
        public string tankLevelPercent { get; set; }
        public string rangeElectric { get; set; }
        public string rangeLiquid { get; set; }
        public int stateOfCharge { get; set; }
        public Fleet[] fleets { get; set; }
        public Assigneddriver assignedDriver { get; set; }
        public Driverpool[] driverPool { get; set; }
        public int serviceTime { get; set; }
        public int serviceDistance { get; set; }
        public string precondError { get; set; }
        public string precondActive { get; set; }
        public string precondAtDeparture { get; set; }
        public string departureTime { get; set; }
        public string departureTimeMode { get; set; }
        public string endOfChargeTime { get; set; }
        public string departureTimeSoc { get; set; }
        public string chargingStatus { get; set; }
        public string maxRange { get; set; }
        public string vizEngineType { get; set; }
        public string vizModel { get; set; }
        public string vizVehicleType { get; set; }
        public string dataSource { get; set; }
        public string bodyType { get; set; }
        public string ignitionState { get; set; }
        public string occupationState { get; set; }
        public Location location { get; set; }
        public Vehiclestate[] vehicleState { get; set; }
        public Motioninfo motionInfo { get; set; }
        public int[] activeProducts { get; set; }
        public Theft theft { get; set; }
    }

    public class Assigneddriver
    {
        public int userId { get; set; }
        public string driverName { get; set; }
        public string driverGivenName { get; set; }
        public string driverPhone { get; set; }
        public string driverPictureUrl { get; set; }
    }

    public class Location
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class Motioninfo
    {
        public int timestamp { get; set; }
        public string eventType { get; set; }
        public float latitude { get; set; }
        public float longitude { get; set; }
        public float heading { get; set; }
    }

    public class Theft
    {
        public string status { get; set; }
    }

    public class Fleet
    {
        public int fleetId { get; set; }
        public string fleetName { get; set; }
    }

    public class Driverpool
    {
        public int userId { get; set; }
        public string userKey { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
    }

    public class Vehiclestate
    {
        public string name { get; set; }
        public Value value { get; set; }
        public string propertyState { get; set; }
        public int timestamp { get; set; }
    }

    public class Value
    {
    }


}