using Mirror;

namespace SP.Runtime.Core.Services.AuthorizationService.Serializers
{
    public static class MapPlacemarkSerializer
    {
        public static void WriteMapPlacemark(this NetworkWriter writer, AuthorizationService.MapPlacemark placemark)
        {
            writer.WriteString(placemark.UniqueId);
            writer.WriteString(placemark.Name);
            writer.WriteString(placemark.Description);
            writer.WriteVector3(placemark.WorldPosition);
            writer.WriteString(placemark.Icon);
            writer.WriteColorNullable(placemark.Color);
        }
     
        public static AuthorizationService.MapPlacemark ReadMapPlacemark(this NetworkReader reader)
        {
            return new AuthorizationService.MapPlacemark(
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadVector3(),
                reader.ReadString(),
                reader.ReadColorNullable());
        }
    }
}