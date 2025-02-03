using BadgerClan.Logic;
using BadgerClan.Logic.Bot;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string url = app.Configuration["ASPNETCORE_URLS"]?.Split(";").Last() ?? throw new Exception("Unable to find URL");
int port = new Uri(url).Port;

Console.Clear();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Welcome to the Sample BadgerClan Bot!");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("The first time you run this program, please run the following two commands:");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\t winget install Microsoft.devtunnel");//DevTunnel explanation: https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview
Console.WriteLine("\t devtunnel user login");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();
Console.WriteLine("Change the code in Program.cs to add custom behavior.");
Console.WriteLine();
Console.WriteLine("Use the following URL to join your bot:");
Console.WriteLine();
Console.Write($"\tLocal:  ");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"{url}");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();
Console.WriteLine("\tCompetition: 1) Start a DevTunnel for this port with the following command:");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\t                devtunnel host -p {port} --allow-anonymous");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine($"\t             2) Copy the \"Connect via browser\" URL from the DevTunnel output");
Console.WriteLine($"\t                (that will be your bot's URL)");
Console.WriteLine();
//Console.WriteLine("In the output from the 'devtunnel host' command, look for the \"Connect via browser:\" URL.  Paste that in the browser as your bot's address");


app.MapGet("/", () => "Sample BadgerClan bot.  Modify the code in Program.cs to change how the bot performs.");

app.MapPost("/", (GameState request) =>
{
  

    app.Logger.LogInformation("Received move request for game {gameId} turn {turnNumber}", request.GameId, request.TurnNumber);
    var myMoves = new List<Move>();
    var myUnits = new List<Unit>();

    foreach (var unit in request.Units)
    {
        if (unit.Team == request.YourTeamId)
        {
            myUnits.Add(unit);
        }
    }
    //request.
    var myteam = myUnits;
    if (myteam is null)
    {
        //myMoves stays empty
    }
    else
    {
        var enemies = request.Units.Where(u => u.Team != request.YourTeamId);
        var squad = request.Units.Where(u => u.Team == request.YourTeamId);

        foreach (var unit in squad.OrderByDescending(u => u.Type == "Knight"))
        {
            var closest = enemies.OrderBy(u => u.Location.Distance(unit.Location)).FirstOrDefault() as Unit;
            var myUnit = BadgerClan.Logic.Unit.Factory(unit.Type, unit.Id, unit.Attack, unit.AttackDistance, unit.Health, unit.MaxHealth, unit.Moves, unit.MaxMoves, unit.Location, unit.Team);
            var myClosest = BadgerClan.Logic.Unit.Factory(closest.Type, closest.Id, closest.Attack, closest.AttackDistance, closest.Health, closest.MaxHealth, closest.Moves, closest.MaxMoves, closest.Location, closest.Team);
            if (closest != null)
            {

                if (unit.Type == "Archer" && closest.Location.Distance(unit.Location) == 1)
                {
                    //Archers run away from knights
                    var target = myUnit.Location.Away(closest.Location);
                    myMoves.Add(new Move(MoveType.Walk, myUnit.Id, target));
                    myMoves.Add(SharedMoves.AttackClosest(myUnit, myClosest));
                }
                else if (closest.Location.Distance(unit.Location) <= unit.AttackDistance)
                {
                    myMoves.Add(SharedMoves.AttackClosest(myUnit, myClosest));
                    myMoves.Add(SharedMoves.AttackClosest(myUnit, myClosest));
                }
                // if archer closer than knight, stay still + let knight move ahead
                if (myUnit.Type == "Archer")
                {
                    // calculate my position relative to team relative to closest enemy
                    foreach (var soldier in request.Units.Where(u => u.Team == request.YourTeamId))
                    {

                        if (soldier.Type == "knight" && soldier.Location.Distance(closest.Location) > unit.Location.Distance(closest.Location))
                        {
                            // wait for knight to get closer to enemy than you are.
                        }
                        else
                        {
                            myMoves.Add(StepToClosest(unit, closest, request));
                        }
                    }
                }
                else
                {
                    myMoves.Add(StepToClosest(unit, closest, request));
                }
            }
        }
    }

    return new MoveResponse(myMoves);
});

Move StepToClosest(Unit unit, Unit closest, GameState request)
{
    Random rnd = new Random();

    var target = unit.Location.Toward(closest.Location);

    var neighbors = unit.Location.Neighbors();

    while (request.Units.Any(u => u.Location == target))
    {
        if (neighbors.Any())
        {
            var i = rnd.Next(0, neighbors.Count() - 1);
            target = neighbors[i];
            neighbors.RemoveAt(i);
        }
        else
        {
            neighbors = unit.Location.MoveEast(1).Neighbors();
        }
    }

    var move = new Move(MoveType.Walk, unit.Id, target);
    return move;
}



app.Run();

public record GameState(IEnumerable<Unit> Units, IEnumerable<int> TeamIds, int YourTeamId, int TurnNumber, string GameId, int BoardSize, int Medpacs);
public record Unit(string Type, int Id, int Attack, int AttackDistance, int Health, int MaxHealth, double Moves, double MaxMoves, Coordinate Location, int Team);