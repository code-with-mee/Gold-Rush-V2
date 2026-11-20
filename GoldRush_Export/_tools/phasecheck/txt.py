import re,sys,html
t=open(sys.argv[1]).read()
t=re.sub(r'<pre class="long"><code>.*?</code></pre>','[CODE]',t,flags=re.S)
t=re.sub(r'</(tr|li|p|h2|h3|div)>','\n',t)
txt=html.unescape(re.sub(r'<[^>]+>',' ',t))
txt=re.sub(r'[ \t]+',' ',txt)
txt=re.sub(r'\n\s*\n+','\n',txt)
print(txt)
