using System;
using System.Collections.Generic;

namespace ShadowProtocol.Game.Dialogue
{
    public class DialogueChoice
    {
        public string ChoiceText { get; set; }
        public string TargetNodeId { get; set; }
        public string RequiredPerkId { get; set; }
        public int RequiredReputationFactionScore { get; set; }
        public string FactionId { get; set; }
        public Action OnSelectedAction { get; set; }
    }

    public class DialogueNode
    {
        public string NodeId { get; set; }
        public string SpeakerName { get; set; }
        public string DialogueText { get; set; }
        public string AudioVoiceoverCue { get; set; }
        public List<DialogueChoice> Choices { get; } = new List<DialogueChoice>();
    }

    public class BranchingDialogueEngine
    {
        private readonly Dictionary<string, DialogueNode> _dialogueNodes = new Dictionary<string, DialogueNode>();
        public DialogueNode CurrentNode { get; private set; }

        public void RegisterNode(DialogueNode node)
        {
            _dialogueNodes[node.NodeId] = node;
        }

        public void StartDialogue(string entryNodeId)
        {
            if (_dialogueNodes.TryGetValue(entryNodeId, out var node))
            {
                CurrentNode = node;
            }
        }

        public bool SelectChoice(int choiceIndex)
        {
            if (CurrentNode == null || choiceIndex < 0 || choiceIndex >= CurrentNode.Choices.Count)
                return false;

            var choice = CurrentNode.Choices[choiceIndex];
            choice.OnSelectedAction?.Invoke();

            if (string.IsNullOrEmpty(choice.TargetNodeId))
            {
                CurrentNode = null; // Dialogue finished
                return true;
            }

            if (_dialogueNodes.TryGetValue(choice.TargetNodeId, out var nextNode))
            {
                CurrentNode = nextNode;
                return true;
            }

            CurrentNode = null;
            return true;
        }
    }
}
