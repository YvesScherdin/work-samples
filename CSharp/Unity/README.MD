# UNITY work samples

> [!NOTE]
> ### UNITY code design choices
> 
> In my UNITY code convention I made some design choices that stand against Clean Code principles. Here some examples with reasons:
> - public variables: not a good practice, generally. But for Unity it is a simple way to expose variables to the editor UI for customizability. I decided against the cleaner alternative (using Attributes) for simplicity and to prevent overhead (including the one produced by calling accessor methods).
> - no SLA: in performance-relevant code (e.g. AI) I tend to prevent further method calls that would've been made for mere cosmetic reasons.
> - multiple classes in one file: couples very specific, coherent and slick logic (and data) within one class (e.g. simple state machine logic)