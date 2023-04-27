#NOTES

## Naming convention

### Mixed cases

For my project, I decided to mix upper camel case and snake case for complex class names with many sub class variants. I have not seen this yet in other projects. I experimented with this and my findings are that the overall readability and - even more important - definition searchability benefits from it.

#### Examples:

- AIAgent_Citizen
- AIAgent_Demon
- AIAgent_Mage
- AIAgent_MeleeFighter
- ...

Now think of yourself searching for a specific AIAgent, and in the class search field you type "AIAgent_". Now the IDE auto-completion offers you all AIAgent-classes, not including the related classes like AIAgentSettings, AIAgentUtil, AIAgentExtensionsa and so on.

### Abbreviaions

Albeit it is considered a malpractice to make use of abbreviations in class names, I decided to abbreviate certain phrases which would elsewise lead to too long class names, worse readability and MUCH more typing effort.

- AIBS = AIBehaviourState
- AIAS = AIActionState
- AIMS = AIMovementState

#### Examples

In combination with mixed cases, I can now skim all available actions easily with the code definition search field.

- AIAS_Follow // more clumsy variant: AIActionStateFollow or FollowAIActionState
- AIAS_PickUpItem // more clumsy variant: AIActionStatePickUpItem or PickUpItemAIActionState

The more clumsy name variants like AIActionStateFollow might look more clean, but they are less easy to read and comprehend. I find it important to fastly understand code in the long run, even if you have a little bit more effort to understand it in the beginning.

But that is just my view.