// See https://aka.ms/new-console-template for more information
using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

class Program
{
    static void Main()
    {
        string connectionString = GetConnectionString();
        MountainDatabasePackage databasePackage = new MountainDatabasePackage(connectionString);

        //1)	Добавлять информацию.
        //databasePackage.AddMountain("New Mountain", 8000, "New Country", "New Region");
        //databasePackage.ShowClimbingGroupsForMountain(11);
        //2) Редактировать информацию
        //databasePackage.EditMountain(1, "Updated Mountain", 7500, "Updated Country", "Updated Region");
        // 3) Удалять информацию
        //databasePackage.DeleteMountain(2);
        //4)Для каждой горы показать список групп, осуществлявших восхождение, в хронологическом порядке; 
        //databasePackage.ShowClimbingGroupsForAllMountains();
        //5)Предоставить возможность добавления новой вершины, с указанием названия вершины, высоты и страны местоположения; 
        //databasePackage.AddMountain("Everest", 8848, "Nepal", "Himalayas");
        //6)Предоставить возможность изменения данных о вершине, если на нее не было восхождения; 
        //databasePackage.EditClimbing(
        // climbingId: 2,  
        // newMountainId: 11,  
        //newStartDate: new DateTime(2024, 2, 1),  
        // newEndDate: new DateTime(2024, 2, 15));
        //7)Показать список альпинистов, осуществлявших восхождение в указанный интервал дат; 
        // DateTime startDate = new DateTime(2023, 05, 01);
        // DateTime endDate = new DateTime(2023, 06, 01);
        //databasePackage.ShowClimbersByDateRange(startDate, endDate);
        //8)Предоставить возможность добавления нового альпиниста в состав указанной группы; 
        // databasePackage.AddClimberToGroup(1, "John Doe", "123 Mountain Street");
        //9)Показать информацию о количестве восхождений каждого альпиниста на каждую гору; 
        //databasePackage.ShowClimberClimbingCount();
        //10)Предоставить возможность добавления новой группы, указав ее название, вершину, время начала восхождения; 
        //databasePackage.AddClimbingGroup("New Group", 1, DateTime.Now);
        //11)Предоставить информацию о том, сколько альпинистов побывали на каждой горе.
        //databasePackage.ShowClimberCountPerMountain();
    }

    private static string GetConnectionString()
    {
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
        IConfiguration configuration = builder.Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Console.WriteLine($"ConnectionString: {connectionString}");

        return connectionString;
    }

    public class MountainDatabasePackage
    {
        private readonly string connectionString;

        public MountainDatabasePackage(string connectionString)
        {
            this.connectionString = connectionString;
        }


        public void AddMountain(string name, int height, string country, string region)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string insertQuery = "INSERT INTO Mountain (MountainName, Height, Country, Region) VALUES (@MountainName, @Height, @Country, @Region)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@MountainName", name);
                        command.Parameters.AddWithValue("@Height", height);
                        command.Parameters.AddWithValue("@Country", country);
                        command.Parameters.AddWithValue("@Region", region);

                        command.ExecuteNonQuery();
                    }
                    Console.WriteLine("Mountain added successfully.");
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error adding mountain: {ex.Message}");
            }
        }

        public void EditMountain(int mountainId, string newName, int newHeight, string newCountry, string newRegion)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string editQuery = "EditMountain";
                using (SqlCommand command = new SqlCommand(editQuery, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@MountainId", mountainId);
                    command.Parameters.AddWithValue("@NewName", newName);
                    command.Parameters.AddWithValue("@NewHeight", newHeight);
                    command.Parameters.AddWithValue("@NewCountry", newCountry);
                    command.Parameters.AddWithValue("@NewRegion", newRegion);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteMountain(int mountainId)
        {

            DeleteClimbingGroupsForMountain(mountainId);
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string deleteQuery = "DELETE FROM Mountain WHERE MountainId = @MountainId";
                using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@MountainId", mountainId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteClimbingGroupsForMountain(int mountainId)
        {

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string deleteClimbingQuery = "DELETE FROM Climbing WHERE GroupId IN (SELECT GroupId FROM ClimbingGroup WHERE MountainId = @MountainId)";
                using (SqlCommand command = new SqlCommand(deleteClimbingQuery, connection))
                {
                    command.Parameters.AddWithValue("@MountainId", mountainId);
                    command.ExecuteNonQuery();
                }
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string deleteGroupsQuery = "DELETE FROM ClimbingGroup WHERE MountainId = @MountainId";
                using (SqlCommand command = new SqlCommand(deleteGroupsQuery, connection))
                {
                    command.Parameters.AddWithValue("@MountainId", mountainId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ShowClimbingGroupsForAllMountains()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string selectQuery = @"
            SELECT MountainId, ClimbingId, Climbing.GroupId, ClimberId, Climbing.StartDate, Climbing.EndDate, GroupName
            FROM Climbing
            JOIN ClimbingGroup ON Climbing.GroupId = ClimbingGroup.GroupId
            ORDER BY MountainId, Climbing.StartDate";

                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        int currentMountainId = -1;

                        while (reader.Read())
                        {
                            int mountainId = reader.GetInt32(0);
                            int climbingId = reader.GetInt32(1);
                            int groupId = reader.GetInt32(2);
                            int climberId = reader.GetInt32(3);
                            DateTime startDate = reader.GetDateTime(4);
                            DateTime endDate = reader.GetDateTime(5);
                            string groupName = reader.GetString(6);

                            if (mountainId != currentMountainId)
                            {
                                Console.WriteLine($"Climbing Groups for Mountain '{mountainId}':");
                                currentMountainId = mountainId;
                            }

                            Console.WriteLine($"  Climbing ID: {climbingId}, Group ID: {groupId}, Climber ID: {climberId}, Start Date: {startDate}, End Date: {endDate}, Group Name: {groupName}");
                        }
                    }
                }
            }
        }

        public void EditClimbing(int climbingId, int newMountainId, DateTime newStartDate, DateTime newEndDate)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string checkClimbingQuery = "SELECT COUNT(*) FROM Climbing WHERE ClimbingId = @ClimbingId";
                using (SqlCommand checkClimbingCommand = new SqlCommand(checkClimbingQuery, connection))
                {
                    checkClimbingCommand.Parameters.AddWithValue("@ClimbingId", climbingId);
                    int climbingCount = (int)checkClimbingCommand.ExecuteScalar();

                    if (climbingCount == 0)
                    {
                        Console.WriteLine($"Climbing with ID {climbingId} not found.");
                        return;
                    }
                }
                string checkNewMountainClimbingQuery = @"
            SELECT COUNT(*)
            FROM Climbing
            WHERE MountainId = @NewMountainId
                AND ((StartDate >= @NewStartDate AND StartDate <= @NewEndDate)
                    OR (EndDate >= @NewStartDate AND EndDate <= @NewEndDate))";
                using (SqlCommand checkNewMountainClimbingCommand = new SqlCommand(checkNewMountainClimbingQuery, connection))
                {
                    checkNewMountainClimbingCommand.Parameters.AddWithValue("@NewMountainId", newMountainId);
                    checkNewMountainClimbingCommand.Parameters.AddWithValue("@NewStartDate", newStartDate);
                    checkNewMountainClimbingCommand.Parameters.AddWithValue("@NewEndDate", newEndDate);

                    int overlappingClimbingCount = (int)checkNewMountainClimbingCommand.ExecuteScalar();

                    if (overlappingClimbingCount > 0)
                    {
                        Console.WriteLine($"There are overlapping climbs on the new mountain during the specified period.");
                        return;
                    }
                }
                string updateClimbingQuery = @"
            UPDATE Climbing
            SET MountainId = @NewMountainId, StartDate = @NewStartDate, EndDate = @NewEndDate
            WHERE ClimbingId = @ClimbingId";

                using (SqlCommand command = new SqlCommand(updateClimbingQuery, connection))
                {
                    command.Parameters.AddWithValue("@ClimbingId", climbingId);
                    command.Parameters.AddWithValue("@NewMountainId", newMountainId);
                    command.Parameters.AddWithValue("@NewStartDate", newStartDate);
                    command.Parameters.AddWithValue("@NewEndDate", newEndDate);
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Climbing with ID {climbingId} updated successfully.");
                }
            }
        }

        public void ShowClimbersByDateRange(DateTime startDate, DateTime endDate)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string selectQuery = @"
            SELECT DISTINCT C.ClimberId, C.Name, C.Address
            FROM Climbing CL
            JOIN Climber C ON CL.ClimberId = C.ClimberId
            WHERE (CL.StartDate BETWEEN @StartDate AND @EndDate)
               OR (CL.EndDate BETWEEN @StartDate AND @EndDate)
               OR (CL.StartDate < @StartDate AND CL.EndDate > @EndDate)";

                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine($"Climbers who climbed between {startDate} and {endDate}:");
                        Console.WriteLine("-------------------------------------------------");

                        while (reader.Read())
                        {
                            int climberId = reader.GetInt32(0);
                            string name = reader.GetString(1);
                            string address = reader.GetString(2);

                            Console.WriteLine($"Climber ID: {climberId}, Name: {name}, Address: {address}");
                        }
                    }
                }
            }
        }
        public void AddClimberToGroup(int groupId, string climberName, string climberAddress)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string checkGroupQuery = "SELECT COUNT(*) FROM ClimbingGroup WHERE GroupId = @GroupId";
                using (SqlCommand checkGroupCommand = new SqlCommand(checkGroupQuery, connection))
                {
                    checkGroupCommand.Parameters.AddWithValue("@GroupId", groupId);
                    int groupCount = (int)checkGroupCommand.ExecuteScalar();

                    if (groupCount == 0)
                    {
                        Console.WriteLine($"Group with ID {groupId} does not exist.");
                        return;
                    }
                }

                string addClimberQuery = "INSERT INTO Climber (Name, Address) VALUES (@Name, @Address); SELECT SCOPE_IDENTITY();";
                int climberId;
                using (SqlCommand addClimberCommand = new SqlCommand(addClimberQuery, connection))
                {
                    addClimberCommand.Parameters.AddWithValue("@Name", climberName);
                    addClimberCommand.Parameters.AddWithValue("@Address", climberAddress);


                    climberId = Convert.ToInt32(addClimberCommand.ExecuteScalar());
                }
                string addClimberToGroupQuery = "INSERT INTO ClimbingGroupClimber (GroupId, ClimberId) VALUES (@GroupId, @ClimberId)";
                using (SqlCommand addClimberToGroupCommand = new SqlCommand(addClimberToGroupQuery, connection))
                {
                    addClimberToGroupCommand.Parameters.AddWithValue("@GroupId", groupId);
                    addClimberToGroupCommand.Parameters.AddWithValue("@ClimberId", climberId);

                    addClimberToGroupCommand.ExecuteNonQuery();
                    Console.WriteLine($"Climber '{climberName}' added to group with ID {groupId} successfully.");
                }
            }
        }

        public void ShowClimberClimbingCount()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string selectQuery = @"
            SELECT
                C.ClimberId,
                C.Name AS ClimberName,
                M.MountainId,
                M.MountainName,
                COUNT(CL.ClimbingId) AS ClimbingCount
            FROM
                Climber C
            LEFT JOIN
                Climbing CL ON C.ClimberId = CL.ClimberId
            LEFT JOIN
                Mountain M ON CL.MountainId = M.MountainId
            GROUP BY
                C.ClimberId,
                C.Name,
                M.MountainId,
                M.MountainName
            ORDER BY
                C.ClimberId,
                M.MountainId;";

                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("Информация о количестве восхождений каждого альпиниста на каждую гору:");
                        Console.WriteLine("--------------------------------------------------------------");

                        while (reader.Read())
                        {
                            int climberId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            string climberName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                            int mountainId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            string mountainName = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3);
                            int climbingCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

                            Console.WriteLine($"Альпинист ID: {climberId}, Имя: {climberName}, Гора ID: {mountainId}, Название горы: {mountainName}, Количество восхождений: {climbingCount}");
                        }
                    }
                }
            }
        }
        public void AddClimbingGroup(string groupName, int mountainId, DateTime startDate)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string insertQuery = @"
                INSERT INTO ClimbingGroup (GroupName, MountainId, StartDate)
                VALUES (@GroupName, @MountainId, @StartDate);
            ";

                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@GroupName", groupName);
                    command.Parameters.AddWithValue("@MountainId", mountainId);
                    command.Parameters.AddWithValue("@StartDate", startDate);

                    command.ExecuteNonQuery();
                }
            }
        }
        public void ShowClimberCountPerMountain()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string selectQuery = @"
                SELECT
                    M.MountainName,
                    COUNT(DISTINCT C.ClimberId) AS ClimberCount
                FROM
                    Mountain M
                LEFT JOIN
                    Climbing CL ON M.MountainId = CL.MountainId
                LEFT JOIN
                    Climber C ON CL.ClimberId = C.ClimberId
                GROUP BY
                    M.MountainName";

                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("Number of climbers per mountain:");
                        Console.WriteLine("-------------------------------");

                        while (reader.Read())
                        {
                            string mountainName = reader.GetString(0);
                            int climberCount = reader.GetInt32(1);

                            Console.WriteLine($"{mountainName}: {climberCount} climbers");
                        }
                    }
                }
            }
        }
    }
}

        
 