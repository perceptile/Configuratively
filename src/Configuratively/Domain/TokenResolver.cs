using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Dynamic;
using System.Text.RegularExpressions;

using Dynamitey;

namespace Configuratively.Domain
{
    public class TokenResolver
    {
        private const string TOKEN_REGEX = "##.*?##";
        private readonly TokenReferenceTracker _tokenTracker = new TokenReferenceTracker();

        private class TokenReference
        {
            public string Token { get; set; }
            public IList<string> References { get; set; }
            public string Value { get; set; }
        }

        private class TokenReferenceTracker
        {
            private IList<TokenReference> _tracker = new List<TokenReference>();

            public void Add(string token, string reference)
            {
                var existing = _tracker.FirstOrDefault(t => t.Token == token);

                if (existing != null)
                {
                    existing.References.Add(reference);
                }
                else
                {
                    var newToken = new TokenReference
                    {
                        Token = token,
                        References = new List<string> { reference },
                        Value = null
                    };

                    _tracker.Add(newToken);
                }
            }

            public TokenReference GetToken(string tokenName)
            {
                var token = _tracker.FirstOrDefault(t => t.Token == tokenName);
                return (token != null ? token : null);
            }

            public string GetTokenValue(string tokenName)
            {
                var token = _tracker.FirstOrDefault(t => t.Token == tokenName);
                return (token != null ? token.Value : null);
            }

            public void SetTokenValue(string tokenName, string value)
            {
                var token = _tracker.FirstOrDefault(t => t.Token == tokenName);
                if (token != null)
                {
                    token.Value = value;
                }
                else
                {
                    // Pretty sure this needs to be empty, as the token may not yet have been
                    // discovered yet.
                    // TODO: Understand whether this could result in an endless loop in the
                    // event of an undefined token?

                    //throw new Exception(string.Format("Missing token: {0}", tokenName));
                }
            }
        }

        public dynamic ResolveTokens(dynamic repo)
        {
            FindTokens(repo);
            var resolvedRepo = resolveTokens(repo);
            return repo;
        }

        private void FindTokens(dynamic repo)
        {
            string _itemId = string.Empty;
            if (repo is IDictionary<string, object>)
            {
                if (((IDictionary<string, object>)repo).ContainsKey("_id"))
                {
                    object id;
                    bool res = ((IDictionary<string, object>)repo).TryGetValue("_id", out id);
                    if (res)
                    {
                        _itemId = id.ToString();
                    }
                }
            }

            foreach (var item in repo)
            {
                if (item is ExpandoObject)
                {
                    FindTokens(item);
                }
                else if (item is KeyValuePair<string, ExpandoObject>)
                {
                    throw new NotImplementedException("complex-type value found");
                }
                else if (item is KeyValuePair<string, object>)
                {
                    var regex = new Regex(TOKEN_REGEX);
                    var matches = regex.Matches(item.Value.ToString());

                    foreach (var m in matches)
                    {
                        var token = m.ToString();
                        // need a way to derive the fully-qualified namespace of a given key???
                        var reference = repo._id;
                        _tokenTracker.Add(token, reference);
                    }
                }
            }
        }

        /*
         * Token Resolution Overview
         *
         * Given an object graph with a single root node (i.e. not an array of root node),
         * we can recurse through it looking for tokens.
         * 
         * The tokens themselves reference nodes with the repo using the typical dotted object
         * notation.  For example: ##NodeA.child.property##
        */

        private dynamic resolveTokens(ExpandoObject repo)
        {
            // For the initial call the repo's are the same
            return resolveTokens(repo, repo);
        }
        private dynamic resolveTokens(ExpandoObject repo, ExpandoObject parentRepo)
        {
            // keep track of the repo keys we need to update, as we'll not be
            // able to do it whilst iterating through the object graph
            var keysToUpdate = new Dictionary<string, string>();

            foreach (var key in ((IDictionary<string, object>)repo).Keys)
            {
                var itemValue = ((IDictionary<string, object>)repo)[key];

                if (itemValue is ExpandoObject)
                {
                    // recursive call
                    // passing the parentRepo ensures we still have access to the whole repo
                    // which is needed to resolve token values we may encounter
                    var resolvedItem = resolveTokens((ExpandoObject)itemValue, parentRepo);
                }
                else
                {
                    var regex = new Regex(TOKEN_REGEX);

                    // this loop will expand nested tokens
                    while ((regex.Match(itemValue.ToString())).Success)
                    {
                        var matches = regex.Matches(itemValue.ToString());

                        foreach (var m in matches)
                        {
                            if (_tokenTracker.GetTokenValue(m.ToString()) == null)
                            {
                                // Remove the tokenisation markers
                                // TODO: Needs to be driven by the regex constant
                                var token = m.ToString().TrimStart('#').TrimEnd('#');

                                // tokens are 'fully-qualified' references to nodes in the object graph,
                                // this code walks the referenced node making dynamic calls to evaluate
                                // each segment.
                                // (e.g. NodeA.child.property)
                                var tokenSplit = token.ToString().Split('.');

                                dynamic tokenValue = (dynamic)parentRepo;
                                foreach (var t in tokenSplit)
                                {
                                    if (Helpers.HasProperty(tokenValue, t))
                                    {
                                        tokenValue = Dynamic.InvokeGet(tokenValue, t);
                                    }
                                    else
                                    {
                                        // Should we bubble-up an error somehow, so the caller knows about
                                        //  a missing token without having to inspect the output?
                                        tokenValue = string.Format("<ERROR: Undefined token -> '{0}'>", t);
                                    }
                                }
                                _tokenTracker.SetTokenValue(m.ToString(), tokenValue);
                            }

                            var resovledValue = _tokenTracker.GetTokenValue(m.ToString());
                            var updatedValue = itemValue.ToString().Replace(m.ToString(), resovledValue);
                            keysToUpdate[key] = updatedValue;
                            itemValue = updatedValue;
                        }
                    }
                }
            }

            // Now we've finished iterating through we can apply any updates 
            foreach (var key in keysToUpdate.Keys)
            {
                Dynamic.InvokeSet(repo, key, keysToUpdate[key]);
            }

            return repo;
        }


    }
}
