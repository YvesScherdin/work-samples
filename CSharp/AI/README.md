# NOTES on AI classes

## Naming convention

### Mixed cases

For my project, I decided to mix upper camel case and snake case for complex class names with many sub class variants. I have not seen this yet in other projects. I experimented with this and my findings are that the overall readability and - even more important - definition searchability benefits from it.

#### Examples:

- AIAgent_Citizen
- AIAgent_Demon
- AIAgent_Mage
- AIAgent_MeleeFighter
- ...

Now think of yourself searching for a specific AIAgent, and in the class search field you type "AIAgent_". Now the IDE auto-completion offers you all AIAgent-classes, not included potentially related classes like AIAgentSettings, AIAgentUtil, AIAgentExtensionsa and alike. This would be different if we had no underscore in the names to distinguish the sub variant. A class name AIAgentCitizen would be cluttered up with AIAgentUtil, whereas the citizen class would be a sub variant and the util would not.

### Abbreviaions

Albeit it is considered a bad habit to make use of abbreviations in class names, I decided to abbreviate certain phrases which would elsewise lead to too long class names, worse readability and MUCH more typing effort.

- AIAS = AIActionState
- AIMS = AIMovementState
- AIBS = AIBehaviourState
 
In combination with mixed cases, I can now list all available state classes easily with the code definition search, be it action states (AIAS) or behaviour states (AIBS).

#### Examples

- AIAS_Follow // more clumsy variant: AIActionStateFollow or FollowAIActionState
- AIAS_PickUpItem // more clumsy variant: AIActionStatePickUpItem or PickUpItemAIActionState
- AIBS_Battle = battle ai behaviour state

#### Comments to examples

The more clumsy name variants like AIActionStateFollow might look more clean, but they are less easy to read and comprehend. I find it important to fastly understand code in the long run, even if you have a little bit more effort to understand it in the beginning. And here, underscores are vital to create a visual distinction: AIASFollow would be painful to decipher in contrast to AIAS_Follow.

Please note also, that I cannot simply name AIBS_Battle AIBehaviour_Battle, since I need the distiction between Behaviour and BehaviourState. Architecturally, "Behaviour" has already a meaning in the code framework of Unity. And AIBehaviourState_Battle would be a rather long class name again.