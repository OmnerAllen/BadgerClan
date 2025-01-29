using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadgerClan.Logic.Bot
{
    public class OmBot : IBot
    {

        public Task<List<Move>> PlanMovesAsync(GameState state)
        {
            var myteam = state.TeamList.FirstOrDefault(t => t.Id == state.CurrentTeamId);
            if (myteam is null)
                return Task.FromResult(new List<Move>());
            var enemies = state.Units.Where(u => u.Team != state.CurrentTeamId);
            var squad = state.Units.Where(u => u.Team == state.CurrentTeamId);

            var moves = new List<Move>();

            foreach (var unit in squad.OrderByDescending(u => u.Type == "Knight"))
            {
                var closest = enemies.OrderBy(u => u.Location.Distance(unit.Location)).FirstOrDefault();

                if (closest != null)
                {

                    if (unit.Type == "Archer" && closest.Location.Distance(unit.Location) == 1)
                    {
                        //Archers run away from knights
                        var target = unit.Location.Away(closest.Location);
                        moves.Add(new Move(MoveType.Walk, unit.Id, target));
                        moves.Add(SharedMoves.AttackClosest(unit, closest));
                    }
                    else if (closest.Location.Distance(unit.Location) <= unit.AttackDistance)
                    {
                        moves.Add(SharedMoves.AttackClosest(unit, closest));
                        moves.Add(SharedMoves.AttackClosest(unit, closest));
                    }
                    // if archer closer than knight, stay still + let knight move ahead
                    if (unit.Type == "Archer")
                    {
                        // calculate my position relative to team relative to closest enemy
                        foreach (var soldier in state.Units.Where(u => u.Team == state.CurrentTeamId))
                        {
                            
                            if (soldier.Type == "knight" && soldier.Location.Distance(closest.Location) > unit.Location.Distance(closest.Location) )
                            {
                                    // wait for knight to get closer to enemy than you are.
                            }
                        }
                    }
                    else
                    {
                        moves.Add(SharedMoves.StepToClosest(unit, closest, state));

                    }
                }

            }
            return Task.FromResult(moves);
        }
    }
}
