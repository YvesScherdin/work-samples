using MageGame.Skills;

namespace MageGame.AI.Data.Modules
{
    public interface ISkillProvidingModule
    {
        ActionSkill GetSkill();
    }
}
