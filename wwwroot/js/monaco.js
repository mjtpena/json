// Monaco Editor JavaScript Integration

let editors = new Map();

window.initializeMonacoEditor = async (containerId, language = 'json', theme = 'vs-dark') => {
    try {
        // Wait for Monaco to be available
        await new Promise((resolve) => {
            if (window.monaco) {
                resolve();
            } else {
                const checkMonaco = () => {
                    if (window.monaco) {
                        resolve();
                    } else {
                        setTimeout(checkMonaco, 100);
                    }
                };
                checkMonaco();
            }
        });

        const container = document.getElementById(containerId);
        if (!container) {
            throw new Error(`Container with id '${containerId}' not found`);
        }

        // Dispose existing editor if any
        if (editors.has(containerId)) {
            editors.get(containerId).dispose();
        }

        const editor = monaco.editor.create(container, {
            value: '',
            language: language,
            theme: theme,
            automaticLayout: true,
            minimap: { enabled: true },
            scrollBeyondLastLine: false,
            fontSize: 14,
            fontFamily: 'JetBrains Mono, Fira Code, Monaco, Consolas, monospace',
            lineNumbers: 'on',
            renderWhitespace: 'selection',
            folding: true,
            foldingStrategy: 'indentation',
            showFoldingControls: 'always',
            wordWrap: 'on',
            bracketPairColorization: { enabled: true },
            guides: {
                bracketPairs: true,
                bracketPairsHorizontal: true,
                highlightActiveBracketPair: true,
                indentation: true
            },
            suggest: {
                showKeywords: true,
                showSnippets: true,
                showClasses: true,
                showFunctions: true,
                showVariables: true
            },
            quickSuggestions: {
                other: true,
                comments: false,
                strings: false
            },
            parameterHints: { enabled: true },
            autoIndent: 'full',
            formatOnPaste: true,
            formatOnType: true,
            renderValidationDecorations: 'on'
        });

        // Store editor reference
        editors.set(containerId, editor);

        // Add context menu actions
        editor.addAction({
            id: 'format-document',
            label: 'Format Document',
            keybindings: [monaco.KeyMod.Shift | monaco.KeyMod.Alt | monaco.KeyCode.KeyF],
            contextMenuGroupId: 'formatting',
            contextMenuOrder: 1.5,
            run: () => {
                editor.getAction('editor.action.formatDocument').run();
            }
        });

        editor.addAction({
            id: 'copy-all',
            label: 'Copy All',
            keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyMod.Shift | monaco.KeyCode.KeyC],
            contextMenuGroupId: 'cutcopypaste',
            contextMenuOrder: 2.5,
            run: () => {
                const value = editor.getValue();
                navigator.clipboard.writeText(value).catch(() => {
                    // Fallback
                    window.copyToClipboardFallback(value);
                });
            }
        });

        return true;
    } catch (error) {
        console.error('Failed to initialize Monaco Editor:', error);
        return false;
    }
};

window.setMonacoEditorValue = (containerId, value) => {
    try {
        const editor = editors.get(containerId);
        if (editor) {
            editor.setValue(value || '');
            return true;
        }
        return false;
    } catch (error) {
        console.error('Failed to set Monaco Editor value:', error);
        return false;
    }
};

window.getMonacoEditorValue = (containerId) => {
    try {
        const editor = editors.get(containerId);
        return editor ? editor.getValue() : '';
    } catch (error) {
        console.error('Failed to get Monaco Editor value:', error);
        return '';
    }
};

window.setMonacoEditorTheme = (theme = 'vs-dark') => {
    try {
        if (window.monaco) {
            monaco.editor.setTheme(theme);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Failed to set Monaco Editor theme:', error);
        return false;
    }
};

window.formatMonacoDocument = (containerId) => {
    try {
        const editor = editors.get(containerId);
        if (editor) {
            editor.getAction('editor.action.formatDocument').run();
            return true;
        }
        return false;
    } catch (error) {
        console.error('Failed to format Monaco document:', error);
        return false;
    }
};

window.setMonacoEditorLanguage = (containerId, language) => {
    try {
        const editor = editors.get(containerId);
        if (editor) {
            const model = editor.getModel();
            if (model) {
                monaco.editor.setModelLanguage(model, language);
                return true;
            }
        }
        return false;
    } catch (error) {
        console.error('Failed to set Monaco Editor language:', error);
        return false;
    }
};

window.disposeMonacoEditor = (containerId) => {
    try {
        const editor = editors.get(containerId);
        if (editor) {
            editor.dispose();
            editors.delete(containerId);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Failed to dispose Monaco Editor:', error);
        return false;
    }
};

// Auto-resize editors when window resizes
window.addEventListener('resize', () => {
    editors.forEach(editor => {
        editor.layout();
    });
});

// Custom JSON validation and suggestions
window.setupJsonValidation = () => {
    if (window.monaco) {
        // Configure JSON language features
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            allowComments: false,
            schemas: [],
            enableSchemaRequest: true,
            schemaValidation: 'error',
            schemaRequest: 'ignore'
        });

        // Add custom completions
        monaco.languages.registerCompletionItemProvider('json', {
            provideCompletionItems: (model, position) => {
                const word = model.getWordUntilPosition(position);
                const range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: word.endColumn
                };

                return {
                    suggestions: [
                        {
                            label: 'string',
                            kind: monaco.languages.CompletionItemKind.Snippet,
                            insertText: '"${1:key}": "${2:value}"',
                            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                            documentation: 'String key-value pair',
                            range: range
                        },
                        {
                            label: 'number',
                            kind: monaco.languages.CompletionItemKind.Snippet,
                            insertText: '"${1:key}": ${2:0}',
                            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                            documentation: 'Number key-value pair',
                            range: range
                        },
                        {
                            label: 'boolean',
                            kind: monaco.languages.CompletionItemKind.Snippet,
                            insertText: '"${1:key}": ${2|true,false|}',
                            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                            documentation: 'Boolean key-value pair',
                            range: range
                        },
                        {
                            label: 'object',
                            kind: monaco.languages.CompletionItemKind.Snippet,
                            insertText: '"${1:key}": {\n\t"${2:property}": "${3:value}"\n}',
                            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                            documentation: 'Object key-value pair',
                            range: range
                        },
                        {
                            label: 'array',
                            kind: monaco.languages.CompletionItemKind.Snippet,
                            insertText: '"${1:key}": [\n\t"${2:item}"\n]',
                            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                            documentation: 'Array key-value pair',
                            range: range
                        }
                    ]
                };
            }
        });
    }
};

// Initialize JSON validation when Monaco is ready
if (window.monaco) {
    window.setupJsonValidation();
} else {
    const checkMonaco = () => {
        if (window.monaco) {
            window.setupJsonValidation();
        } else {
            setTimeout(checkMonaco, 100);
        }
    };
    checkMonaco();
}