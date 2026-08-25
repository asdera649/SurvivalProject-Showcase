using Mirror;

namespace SP.Runtime.Core.Services.AuthorizationService.Serializers
{
    // Проблема: мы отпраляем RespawnPoint через NetworkMessage, 
    // NetworkMessage не сериализует приватные поля, из за чего мы лишаемся возможности
    // инкапсулировать
    // Решение: написать кастомный сериализатор RespawnPointSerializer, и для сборки класса
    // у получателя добавить перегрузку конструктора RespawnPoint(string uniqueId, string name, float cooldown)
    public static class RespawnPointSerializer
    {
        public static void WriteRespawnPoint(this NetworkWriter writer, AuthorizationService.RespawnPoint respawnPoint)
        {
            writer.WriteString(respawnPoint.UniqueId);
            writer.WriteString(respawnPoint.Name);
            writer.WriteFloat(respawnPoint.Cooldown);
        }
     
        public static AuthorizationService.RespawnPoint ReadRespawnPoint(this NetworkReader reader)
        {
            return new AuthorizationService.RespawnPoint(
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadFloat());
        }
    }
}