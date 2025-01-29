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
Console.WriteLine("Change the code in Program.cs to add custom behavior.");
Console.WriteLine("If you're running this locally, use the following URL to join your bot:");
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\t{url}");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();
Console.WriteLine("For the competition, start a DevTunnel for this port with the following commands:");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\t winget install Microsoft.devtunnel");
Console.WriteLine("\t [ restart your command line after installing devtunnel ]");
Console.WriteLine("\t devtunnel user login");
Console.WriteLine($"\t devtunnel host -p {port} --allow-anonymous");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();
Console.WriteLine("In the output from the 'devtunnel host' command, look for the \"Connect via browser:\" URL.  Paste that in the browser as your bot's address");


app.MapGet("/", () => "Sample BadgerClan bot.  Modify the code in Program.cs to change how the bot performs.");

app.MapPost("/", (MoveRequest request) =>
{
    app.Logger.LogInformation("Received move request for game {gameId} turn {turnNumber}", request.GameId, request.TurnNumber);
    var myMoves = new List<Move>();
    var myUnits = new List<Unit>();
    OmBot om = new OmBot();

    
    foreach (var unit in request.Units)
    {
        if (unit.Team == request.YourTeamId)
        {
            Unit myConvertedUnit = Unit.Factory(unit.Type, unit.Id, unit.Attack, unit.AttackDistance, unit.Health, unit.MaxHealth, unit.Moves, unit.MaxMoves, unit.Location, unit.Team);
            myUnits.Add(myConvertedUnit);
        }
    }
    //request.
    //var myteam = request.TeamList.FirstOrDefault(t => t.Id == request.YourTeamId);
    var myteam = myUnits;
    if (myteam is null)
    {
       //myMoves stays empty
    }
    else 
    { 
        var enemies = request.Units.Where(u => u.Team != request.YourTeamId);
        var squad = request.Units.Where(u => u.Team == request.YourTeamId);

        var moves = new List<Move>();

        foreach (var unit in squad.OrderByDescending(u => u.Type == "Knight"))
        {
            var closest = enemies.OrderBy(u => u.Location.Distance(unit.Location)).FirstOrDefault();
            var myUnit = Unit.Factory(unit.Type, unit.Id, unit.Attack, unit.AttackDistance, unit.Health, unit.MaxHealth, unit.Moves, unit.MaxMoves, unit.Location, unit.Team);
            var myClosest = Unit.Factory(closest.Type, closest.Id, closest.Attack, closest.AttackDistance, closest.Health, closest.MaxHealth, closest.Moves, closest.MaxMoves, closest.Location, closest.Team);

            if (closest != null)
            {

                if (unit.Type == "Archer" && closest.Location.Distance(unit.Location) == 1)
                {
                    //Archers run away from knights
                    var target = myUnit.Location.Away(myClosest.Location);
                    moves.Add(new Move(MoveType.Walk, myUnit.Id, target));
                    moves.Add(SharedMoves.AttackClosest(myUnit, myClosest));
                }
                else if (closest.Location.Distance(unit.Location) <= unit.AttackDistance)
                {
                    moves.Add(SharedMoves.AttackClosest(myUnit, myClosest));
                    moves.Add(SharedMoves.AttackClosest(myUnit, myClosest));
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
                    }
                }
                else
                {
                    moves.Add(StepToClosest(myUnit, myClosest, request));

                }
            }
        }

    }


    // ***************************************************************************
    // ***************************************************************************
    // **
    // ** Your code goes right here.
    // ** Look in the request object to see the game state.
    // ** Then add your moves to the myMoves list.
    // **
    // ***************************************************************************
    // ***************************************************************************
    return new MoveResponse(myMoves);
});
Move StepToClosest(Unit unit, Unit closest, MoveRequest request)
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
