# Code the Park - all sounds and music made by us with code (no samples, no licence needed).
import numpy as np, wave, os
SR=44100
OUT='/mnt/user-data/outputs/audio'; os.makedirs(OUT,exist_ok=True)
rng=np.random.default_rng(7)

def save(name,x,peak=0.9):
    x=np.asarray(x,dtype=np.float64); m=np.max(np.abs(x)) or 1; x=x/m*peak
    with wave.open(f'{OUT}/{name}.wav','wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((x*32767).astype('<i2').tobytes())
    print(name, round(len(x)/SR,2),'s')

def t_(d): return np.arange(int(d*SR))/SR
def env(n,a=0.005,r=0.1,total=None):
    e=np.ones(n); na=max(1,int(a*SR)); nr=max(1,int(r*SR))
    e[:na]=np.linspace(0,1,na); e[-nr:]*=np.linspace(1,0,nr); return e
def freq(note):   # midi
    return 440*2**((note-69)/12)
def bell(f,d,dec=6):
    t=t_(d); x=np.sin(2*np.pi*f*t)+0.5*np.sin(2*np.pi*f*2.76*t)*np.exp(-t*dec*1.5)+0.25*np.sin(2*np.pi*f*5.4*t)*np.exp(-t*dec*3)
    return x*np.exp(-t*dec)*env(len(t),0.002,0.02)
def organ(f,d,vib=True):     # calliope / steam organ pipe
    t=t_(d); v=1+ (0.004*np.sin(2*np.pi*5.5*t) if vib else 0)
    ph=2*np.cumsum(np.pi*f*v)/SR
    x=np.sin(ph)+0.45*np.sin(2*ph)+0.25*np.sin(3*ph)+0.12*np.sin(4*ph)
    x+=0.03*rng.standard_normal(len(t))*np.exp(-t*20)      # breath at the start
    return x*env(len(t),0.02,min(0.06,d/3))
def mix(buf,x,start):
    i=int(start*SR); j=min(len(buf),i+len(x)); buf[i:j]+=x[:j-i]

# ---------------- short sound effects ----------------
c=np.zeros(int(0.5*SR)); mix(c,bell(freq(83),0.35,9),0); mix(c,bell(freq(88),0.4,7),0.07); save('sfx_coin',c)
g=np.zeros(int(0.9*SR))
for k,n in enumerate([84,88,91,96,100]): mix(g,bell(freq(n),0.5,6)*0.8,k*0.06)
save('sfx_gem',g)
tl=np.zeros(int(0.5*SR))
for k in range(4):
    cl=rng.standard_normal(int(0.03*SR))*np.exp(-t_(0.03)*120); mix(tl,cl*0.6,k*0.05)
mix(tl,bell(freq(79),0.35,10)*0.7,0.2); save('sfx_tool',tl)
ok=np.zeros(int(1.1*SR))
for k,n in enumerate([72,76,79]): mix(ok,bell(freq(n),0.4,7),k*0.11)
mix(ok,bell(freq(84),0.8,4),0.33); mix(ok,bell(freq(88),0.8,4)*0.6,0.33); save('sfx_correct',ok)
wr=np.zeros(int(0.8*SR))
for k,n in enumerate([55,50]):
    t=t_(0.32); f=freq(n); x=(np.sin(2*np.pi*f*t)+0.3*np.sin(4*np.pi*f*t))*np.exp(-t*7)*env(len(t),0.01,0.05); mix(wr,x,k*0.22)
save('sfx_wrong',wr,0.7)
t=t_(0.06); save('sfx_click',np.sin(2*np.pi*1500*t)*np.exp(-t*80)+0.3*rng.standard_normal(len(t))*np.exp(-t*200),0.6)
t=t_(0.7); f=300+900*(t/0.7)**1.5; run=np.sin(2*np.pi*np.cumsum(f)/SR)*env(len(t),0.01,0.15)+0.3*np.sin(4*np.pi*np.cumsum(f)/SR)*env(len(t),0.01,0.15)
save('sfx_run',run,0.6)
# ride power-on: electric hum rising + clunk
t=t_(1.8); f=40+60*np.clip(t/1.2,0,1); hum=np.sign(np.sin(2*np.pi*np.cumsum(f)/SR))*0.25+np.sin(2*np.pi*np.cumsum(f*2)/SR)*0.3
hum*=np.clip(t/1.2,0,1)*np.exp(-np.clip(t-1.3,0,None)*6)
pw=hum.copy(); clunk=rng.standard_normal(int(0.15*SR))*np.exp(-t_(0.15)*40)+np.sin(2*np.pi*70*t_(0.15))*np.exp(-t_(0.15)*25)*2
mix(pw,clunk*0.8,0.02); mix(pw,clunk*0.6,1.25); save('sfx_power_on',pw,0.7)
# gate doors: metal creak + clank
t=t_(1.6); f=180+40*np.sin(2*np.pi*1.3*t)+20*rng.standard_normal(len(t)).cumsum()/SR*5
cr=np.sin(2*np.pi*np.cumsum(f)/SR)*(0.5+0.5*np.sin(2*np.pi*23*t))*env(len(t),0.1,0.3)*0.5
mix(cr,bell(freq(45),0.8,5)*0.8,1.4) if False else None
door=np.zeros(int(2.2*SR)); mix(door,cr,0); mix(door,bell(freq(43),0.7,6),1.5); save('sfx_gate_open',door,0.7)
# fireworks
t=t_(1.0); f=900+1500*t; wh=np.sin(2*np.pi*np.cumsum(f)/SR)*0.25+0.4*rng.standard_normal(len(t))*np.linspace(0.2,1,len(t))*0.3
wh*=env(len(t),0.05,0.05); save('sfx_firework_launch',wh,0.5)
n=int(2.2*SR); t=np.arange(n)/SR; boom=rng.standard_normal(n)
from numpy.fft import rfft,irfft
B=rfft(boom); fr=np.fft.rfftfreq(n,1/SR); B*=1/(1+(fr/300)**2); boom=irfft(B,n)*np.exp(-t*4)
crk=np.zeros(n)
for _ in range(90):
    p=rng.uniform(0.25,1.9); mix(crk,rng.standard_normal(int(0.004*SR))*rng.uniform(0.2,1),p)
save('sfx_firework_boom',boom/np.max(np.abs(boom))+crk*0.5*np.exp(-t*1.2),0.85)
# celebration fanfare
fan=np.zeros(int(3.2*SR)); seq=[(72,0,0.2),(72,0.2,0.2),(72,0.4,0.2),(76,0.6,0.5),(74,1.1,0.2),(76,1.3,0.2),(79,1.5,1.4)]
for n_,s,d in seq:
    mix(fan,organ(freq(n_),d)*0.8,s); mix(fan,organ(freq(n_-12),d)*0.4,s)
for n_ in [60,64,67,72]: mix(fan,organ(freq(n_),1.5)*0.35,1.5)
save('sfx_celebrate',fan,0.85)

# ---------------- music loops (original tunes) ----------------
C,D,E,F,G,A,B=60,62,64,65,67,69,71
CH={'C':[60,64,67],'F':[65,69,72],'G':[67,71,74],'G7':[67,71,74,77],'Am':[69,72,76],'Dm':[62,65,69]}
def waltz():                           # carousel: calliope waltz, 3/4, 150 bpm, 16 bars
    beat=0.4; bars=['C','C','G','G','G7','G7','C','C','F','F','C','Am','Dm','G7','C','C']
    mel=[[E+12,G+12,E+12],[C+12,0,G],[D+12,F+12,D+12],[B,0,G],[F+12,E+12,D+12],[B,C+12,D+12],[E+12,0,0],[C+12,D+12,E+12],
         [F+12,A+12,F+12],[C+12,0,A],[G+12,E+12,C+12],[A,C+12,E+12],[D+12,F+12,A+12],[G+12,F+12,D+12],[C+12,0,G],[C+12,0,0]]
    buf=np.zeros(int(len(bars)*3*beat*SR)+SR)
    for i,(ch,m) in enumerate(zip(bars,mel)):
        s=i*3*beat; root=CH[ch][0]-24
        mix(buf,organ(freq(root),beat*0.9)*0.55,s)
        for b in (1,2):
            for n_ in CH[ch][:3]: mix(buf,organ(freq(n_-12),beat*0.55)*0.18,s+b*beat)
        k=0
        while k<3:
            if m[k]:
                d=1
                while k+d<3 and m[k+d]==0: d+=1
                mix(buf,organ(freq(m[k]),beat*d*0.92)*0.5,s+k*beat)
                mix(buf,bell(freq(m[k]+12),0.3,9)*0.08,s+k*beat)
                k+=d
            else: k+=1
    return buf[:int(len(bars)*3*beat*SR)]
def musicbox():                        # Ferris wheel: gentle music box, 3/4, 100 bpm, 16 bars
    beat=0.6; bars=['F','F','C','C','Dm','Dm','C','C','F','F','Am','Am','Dm','C','F','F']
    CHm={'F':[65,69,72],'C':[60,64,67],'Dm':[62,65,69],'Am':[57,60,64]}
    mel=[77,81,84, 79,76,72, 74,77,81, 79,76,72, 77,81,84, 86,84,81, 79,77,76, 74,72,0]
    buf=np.zeros(int(len(bars)*3*beat*SR)+SR)
    for i,ch in enumerate(bars):
        s=i*3*beat; notes=CHm[ch]
        for b in range(3): mix(buf,bell(freq(notes[b]),1.0,4)*0.25,s+b*beat)
        mix(buf,bell(freq(notes[0]-12),1.6,2.5)*0.3,s)
        n_=mel[(i*3)%len(mel)]
        if n_: mix(buf,bell(freq(n_+12),1.2,3)*0.35,s)
        n2=mel[(i*3+1)%len(mel)]
        if n2: mix(buf,bell(freq(n2+12),1.0,3)*0.25,s+2*beat)
    return buf[:int(len(bars)*3*beat*SR)]
def polka():                           # roller coaster: fast fun polka, 2/4, 150 bpm, 32 bars
    beat=0.4; prog=['C','G','G','C','C','G','G7','C','F','C','G','C','F','C','G7','C']*2
    mel=[72,76,79,76, 74,71,67,71, 74,77,74,71, 72,0,67,0, 72,76,79,84, 83,79,74,71, 72,74,76,77, 79,0,0,0,
         77,81,77,72, 76,79,76,72, 74,79,77,74, 72,76,72,67, 77,76,74,72, 76,74,72,71, 74,77,76,74, 72,0,67,0]
    buf=np.zeros(int(len(prog)*2*beat*SR)+SR)
    for i,ch in enumerate(prog):
        s=i*2*beat; root=CH[ch][0]-24
        mix(buf,organ(freq(root),beat*0.5)*0.5,s); mix(buf,organ(freq(CH[ch][2]-24),beat*0.5)*0.4,s+beat)
        for b in (0.5,1.5):
            for n_ in CH[ch][:3]: mix(buf,organ(freq(n_-12),beat*0.3)*0.14,s+b*beat)
            hh=rng.standard_normal(int(0.05*SR))*np.exp(-t_(0.05)*90)*0.12; mix(buf,hh,s+b*beat)
        for q in range(4):
            n_=mel[(i*4+q)%len(mel)]
            if n_: mix(buf,organ(freq(n_),beat*0.45)*0.45,s+q*beat/2)
    return buf[:int(len(prog)*2*beat*SR)]
def crickets():                        # night park ambience, 20 s loop
    n=20*SR; buf=np.zeros(n)
    for _ in range(140):
        f=rng.uniform(3800,5200); p=rng.uniform(0,19.5); amp=rng.uniform(0.05,0.25)
        for k in range(rng.integers(2,5)):
            t=t_(0.035); mix(buf,np.sin(2*np.pi*f*t)*np.sin(np.pi*t/0.035)*amp,p+k*0.06)
    wind=rng.standard_normal(n); W=rfft(wind); fr=np.fft.rfftfreq(n,1/SR); W*=1/(1+(fr/250)**2); wind=irfft(W,n)
    wind=wind/np.max(np.abs(wind))*0.12*(0.6+0.4*np.sin(2*np.pi*np.arange(n)/n))
    return buf+wind
save('music_carousel',waltz(),0.8); save('music_ferriswheel',musicbox(),0.8); save('music_coaster',polka(),0.8)
save('ambience_night',crickets(),0.5)

# ---------------- Codey the robot (cute beeps, made by us) ----------------
def blip(f0,f1,d,wav='sq'):
    t=t_(d); f=np.linspace(f0,f1,len(t)); ph=2*np.pi*np.cumsum(f)/SR
    x=np.sin(ph)+0.35*np.sign(np.sin(ph))*0.5+0.2*np.sin(2*ph)
    x*= (1+0.3*np.sin(2*np.pi*30*t))          # little robotic wobble
    return x*env(len(t),0.004,0.02)
syl=[(700,900,0.07),(900,650,0.08),(1100,1300,0.06),(600,800,0.09),(1200,900,0.07),(800,1100,0.06),(950,950,0.08),(650,600,0.07)]
for k,(a,b,d) in enumerate(syl): save(f'robot_talk_{k}',blip(a,b,d),0.6)
h=np.zeros(int(0.8*SR))
for k,(a,b) in enumerate([(800,1100),(1000,1400),(1300,1900)]): mix(h,blip(a,b,0.11),k*0.12)
mix(h,blip(1500,2200,0.25)*0.8,0.38); save('robot_happy',h,0.7)
sd=np.zeros(int(1.0*SR)); mix(sd,blip(700,500,0.25),0); mix(sd,blip(500,300,0.45),0.28); save('robot_sad',sd,0.6)
# soft hover hum loop (2 s, seamless: whole number of cycles)
t=t_(2.0); hum=np.sin(2*np.pi*110*t)+0.4*np.sin(2*np.pi*220*t)+0.15*np.sin(2*np.pi*331*t)
hum*=0.8+0.2*np.sin(2*np.pi*1.0*t)
nz=rng.standard_normal(len(t)); NZ=rfft(nz); fr=np.fft.rfftfreq(len(t),1/SR); NZ*=1/(1+(fr/400)**2); nz=irfft(NZ,len(t))
save('robot_hover',hum/np.max(np.abs(hum))+nz/np.max(np.abs(nz))*0.15,0.5)

# ---------------- soft shimmer loop for coins / gems / tools (heard only when close) ----------------
n=int(3.0*SR); sh=np.zeros(n)
notes=[88,91,95,96,100,103]
for k in range(14):
    p=k*3.0/14+rng.uniform(0,0.08); x=bell(freq(int(rng.choice(notes))),0.6,7)*rng.uniform(0.3,0.7)
    i=int(p*SR); seg=x[:n-i] if i+len(x)>n else x; sh[i:i+len(seg)]+=seg
    if i+len(x)>n: sh[:i+len(x)-n]+=x[n-i:]                  # wrap the tail so the loop has no click
save('item_shimmer',sh,0.5)

# ---------------- roller coaster on the track (no music) ----------------
def lowpass_noise(n,fc,seed=None):
    x=rng.standard_normal(n); X=rfft(x); fr=np.fft.rfftfreq(n,1/SR); X*=1/(1+(fr/fc)**2); return irfft(X,n)
n=2*SR; t=np.arange(n)/SR                       # 2 s seamless loop (FFT noise is periodic)
rum=lowpass_noise(n,180)/1.0; rum/=np.max(np.abs(rum))
rum+=0.25*np.sin(2*np.pi*55*t)*(0.7+0.3*np.sin(2*np.pi*2*t))      # heavy wheels on steel
hiss=lowpass_noise(n,2500); hiss/=np.max(np.abs(hiss))
save('coaster_rumble',rum+0.12*hiss,0.8)
# one "clack" = wheels over a rail joint (two quick metal knocks)
ck=np.zeros(int(0.16*SR))
for k,d in enumerate([0,0.055]):
    tt=t_(0.09); knock=(rng.standard_normal(len(tt))*0.6+np.sin(2*np.pi*190*tt)+0.6*np.sin(2*np.pi*620*tt))*np.exp(-tt*55)
    mix(ck,knock*(1 if k==0 else 0.8),d)
save('coaster_clack',ck,0.8)
# chain lift tick (when the train is pulled up the first hill)
tt=t_(0.05); save('coaster_chain',(np.sin(2*np.pi*1400*tt)+0.5*rng.standard_normal(len(tt)))*np.exp(-tt*120),0.6)

# ---------------- softer item sound (v2): a quiet warm hum + one gentle chime every 3 s ----------------
n=int(3.0*SR); t=np.arange(n)/SR
pad=(np.sin(2*np.pi*523.25*t)+0.6*np.sin(2*np.pi*659.25*t)+0.4*np.sin(2*np.pi*783.99*t))   # soft C major chord (seamless: 3 s)
pad*=0.55+0.45*np.sin(2*np.pi*t/3.0-np.pi/2)                                               # slow swell, loops smoothly
ch=np.zeros(n); x=bell(freq(84),1.4,3.5)*0.5; ch[:len(x)]+=x[:n]
save('item_shimmer_soft',pad*0.25+ch,0.35)

# ---------------- crystal sound (v3): a few tiny glass tinkles, soft and far apart ----------------
def glass(f,d,dec=9):
    t=t_(d)
    x=(np.sin(2*np.pi*f*t)+0.5*np.sin(2*np.pi*f*2.32*t)*np.exp(-t*dec*0.8)
       +0.3*np.sin(2*np.pi*f*4.25*t)*np.exp(-t*dec*1.5)+0.15*np.sin(2*np.pi*f*6.63*t)*np.exp(-t*dec*2.5))
    return x*np.exp(-t*dec)*env(len(t),0.001,0.02)
n=int(4.0*SR); cr=np.zeros(n)
r2=np.random.default_rng(11)
for p in [0.1,0.55,0.75,2.1,2.35,3.3]:                 # small groups of tinkles, with quiet gaps
    f=freq(int(r2.choice([96,98,100,103,105,107]))); x=glass(f,0.6,r2.uniform(7,11))*r2.uniform(0.35,0.8)
    i=int(p*SR); k=min(len(x),n-i); cr[i:i+k]+=x[:k]
    if len(x)>k: cr[:len(x)-k]+=x[k:]                  # wrap: the loop has no click
save('item_crystal',cr,0.3)

# ---------------- magic sparkle (v4): a rising cloud of tiny twinkles + airy shimmer, then quiet ----------------
def tiny(f,d=0.25):
    t=t_(d); return (np.sin(2*np.pi*f*t)+0.3*np.sin(2*np.pi*f*2.01*t))*np.exp(-t*22)*env(len(t),0.001,0.01)
n=int(4.0*SR); mg=np.zeros(n); r3=np.random.default_rng(23)
sweep=1.1
for k in range(38):                                     # grains go up in pitch, like magic dust
    u=k/37; p=0.05+u*sweep+r3.uniform(-0.03,0.03)
    f=2200*2**(u*1.4)*r3.uniform(0.93,1.07)
    a=np.sin(np.pi*u)**0.7*r3.uniform(0.4,1.0)
    mix(mg,tiny(f)*a,p)
air=rng.standard_normal(int((sweep+0.4)*SR)); A=rfft(air); fr=np.fft.rfftfreq(len(air),1/SR)
A*=(fr>4000)*(1/(1+(fr/9000)**2)); air=irfft(A,len(air)); air/=np.max(np.abs(air))
air*=np.sin(np.pi*np.linspace(0,1,len(air)))**2*0.12
mix(mg,air,0.05)
rev=mg.copy()                                           # a small, soft echo tail (simple reverb)
for d,g in [(0.043,0.35),(0.071,0.28),(0.113,0.22),(0.167,0.16),(0.239,0.1)]:
    i=int(d*SR); rev[i:]+=mg[:-i]*g
save('item_magic',rev,0.3)
