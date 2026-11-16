// ------------------------------------------------------------
// Fichier : IInteractable.cs
// Autrice : SS — créé le 11/11/2026
// Rôle    : contrat commun à tout ce que le joueur active avec la
//           touche Interact (E / bouton A) : portes, murs secrets…
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower
{
    public interface IInteractable
    {
        /// <summary>Texte affiché quand le joueur regarde l'objet (ex. « [E] Ouvrir »).</summary>
        string Prompt { get; }

        /// <summary>Appelé quand le joueur appuie sur Interact en regardant l'objet.</summary>
        void Interact(GameObject player);
    }
}
