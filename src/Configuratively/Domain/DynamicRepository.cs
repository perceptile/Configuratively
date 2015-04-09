using System;
using System.Collections.Generic;
using System.Linq;
using Dynamitey;

namespace Configuratively.Domain
{
    public class DynamicRepository
    {
        private readonly string _basePath;
        private Stack<string> _refTracker;

        public dynamic[] Repo { get; set; }

        public DynamicRepository(string basePath)
        {
            this._basePath = basePath;
            if (!this._basePath.EndsWith(@"\"))
            {
                this._basePath = string.Format(@"{0}\", this._basePath);
            }
        }

        public void ProcessAllLinks(IEnumerable<dynamic> items)
        {
            // We wouldn't normally expect to do this operation en-masse
            Repo = items.ToArray();

            for (int i = 0; i < Repo.Length; i++)
            {
                ResolveLinks(Repo[i]);
            }
        }

        public dynamic ResolveLinks(dynamic entry, IEnumerable<string> links, bool isRecursionRoot = true)
        {
            // Reset the circular reference tracker if we are at the entry point of a new recursion stack
            if (isRecursionRoot)
            {
                _refTracker = new Stack<string>();
            }

            foreach (string link in links)
            {
                // Check for a circular reference
                if (_refTracker.Contains(link))
                {
                    throw new CircularReferenceException(link);
                }
                _refTracker.Push(link);

                dynamic linkContent = Repo.FirstOrDefault(t => t._id == link);
                if (linkContent == null)
                {
                    throw new Exception(string.Format("Unable to locate the linked document '{0}'", link));
                }

                var updatedLinkContent = ResolveLinks(linkContent, false);

                foreach (var linkEntry in updatedLinkContent)
                {
                    string linkEntryMemberName = linkEntry.Key;
                    if (!linkEntryMemberName.StartsWith("_") && linkEntryMemberName != "links")
                    {
                        var entryMembers = ((IEnumerable<string>)Dynamic.GetMemberNames(entry, true)).
                                                Where(m => !m.StartsWith("_"));

                        // If the base entry does not contain the current linked entry then just add it
                        if (!entryMembers.Contains(linkEntryMemberName))
                        {
                            // Add the value from the link
                            Dynamic.InvokeSet(entry, linkEntryMemberName, linkEntry.Value);
                        }
                        else
                        {
                            // Start the recursive merge operation
                            dynamic merged = DynamicMerge.DoMerge(Dynamic.InvokeGet(entry, linkEntry.Key), linkEntry.Value);
                            Dynamic.InvokeSet(entry, linkEntry.Key, merged);
                        }
                    }
                }
            }

            return entry;
        }
        private dynamic ResolveLinks(dynamic entry, bool isRecursionRoot = true)
        {
            if (!entry._isLinksResolved)
            {
                if (Helpers.HasProperty(entry, "links") && entry.links != null && entry.links.Length > 0)
                {
                    entry = ResolveLinks(entry, entry.links, isRecursionRoot);
                }

                entry._isLinksResolved = true;
            }
            return entry;
        }
       
    }

    public class CircularReferenceException : Exception
    {
        public CircularReferenceException(string link)
            : base(string.Format("Circular reference detected for the item '{0}'", link)) {}
    }
}
