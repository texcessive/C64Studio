0 :REM---80-ZEICHEN.ZEILEN 
10 :GOSUB25:IFPEEK(44)>8THEN:GOSUB95 
11 :F=3:GOSUB900 
12 :GOSUB41:GOSUB50:IFC<>131GOTO12 
13 :SYS58784:SYS64789:SYS42107 
19 : 
20 A$="Z80-TEXT":PRINTCHR$(47)A$:REM--SAVE 
21 POKE806,60:POKE770,174:POKE771,167:POKE43,254:POKE44,2:POKE997,1 
22 OPEN1,8,15,"S0:"+A$:CLOSE1:SAVEA$,8 
25 IFPEEK(997)THEN:POKE43,1:POKE44,64:POKE997,.:IFPEEK(123)<64THENRUN 
26 POKE806,202:SYS58451:POKE788,52:RETURN 
29 : 
40 :REM--CRSR 
41 :POKEVC+39,F/16:I=.:A=1:X=X-INT(X/40)*40:C=X*8+24 
42 :IFPEEK(198)THEN:GETA$:C=PEEK(512):POKE53269,.:RETURN 
43 :IFITHEN:I=I+1:ON-(I<15)GOTO42:A=1-A 
45 :POKEVC,CAND255:POKEVC+16,-(C>=256):POKEVC+1,Y*8+43:POKEVC+21,A:I=1:GOTO42 
49 : 
50 :IF(CAND96)=.GOTO61:GRAFIK-AUSGABE 
55 :J=INT(Y*40+X):N=GR+J*8:X=X+.5:A=ZS+FNAS(C)*8:D=1-15*(X>INT(X)):B=255-D*15 
56 :POKEBR+J,F:FORI=.TO7:POKEN+I,PEEK(N+I)ANDBORFNA(PEEK(A+I))*D:NEXT:RETURN 
59 : 
60 :REM--S.TASTEN 
61 D=CAND127:IFD=29ORD=20THEN:X=X+.5*(1+2*(C>32ORC=20)):RETURN 
62 IFC=3THEN:A=(ZS/2048)AND1:ZS=ZS+SGN(.1-A)*2048 
63 IFD=13ORD=17THENX=X*-(D<>13):Y=Y-((C=17ORD=13)ANDY<24)+(C=145ANDY>.):RETURN 
64 IFD=18THEN:ZS=INT(ZS/2048)*2048-1024*(C=18) 
65 ON-(D=19)-(C=19)GOTO93,95:IFC=136THEN:F=INT(F/16):F=F-16*(F=.) 
66 POKE780,C:SYS59595:IFPEEK(781)<16THEN:F=(FAND15)+16*PEEK(646) 
67 IFC>132THEN:OND-4GOTO80,81,82 
68 ON-(C<3)GOTO20:RETURN 
69 : 
80 DEFFNA(A)=(AAND2)/2+(AAND24)/4+(AAND64)/8:RETURN 
81 DEFFNA(A)=(AAND1)+(AAND4)/2+(AAND16)/4+(AAND64)/8:RETURN 
82 DEFFNA(A)=(AAND2)/2+(AAND8)/4+(AAND32)/8+(AAND128)/16:RETURN 
89 : 
90 :F=3:REM--CLS 
93 POKE53265,43:N=.:A=GR:E=A+8000:GOSUB100:A=BR:E=A+1000:N=F:GOSUB100 
95 POKE53272,16*3+8:POKE53265,59:X=.:Y=.:RETURN 
99 : 
100 POKEE,N:REM--KOPY(A,E,N) 
101 N=A:A=A+1:GOSUB102:A=N:RETURN 
102 POKE9,PEEK(1):POKE56334,.:POKE1,51:GOSUB104:POKE1,PEEK(9):POKE56334,1:RETURN
104 D=256:B=E-A:C=INT(B/D)*D:POKE781,C/D+1:POKE782,B-C+1AND255:B=A+C:POKE91,B/D 
105 POKE90,B-PEEK(91)*D:B=N+C:POKE89,B/D:POKE88,B-PEEK(89)*D:SYS41964:RETURN 
199 : 
900 :REM--VAR 
910 DIMI,N,A,B,D:GOSUB80 
920 BR=3072:ZS=4096:GR=8192:VC=53248 
930 DEFFNAS(A)=A+64*((A>63)+(A>191))-(A>95ANDA<128)*32+(A=255)*33 
934 DEFFNP(A)=PEEK(A)+PEEK(A+1)*256
940 IFPEEK(44)>8THENRETURN 
949 : 
950 REM--KOPIEREN 
952 N=16384:A=FNP(43)-1:E=FNP(49):POKE9,(N-A)/256:GOSUB104 
953 POKE44,PEEK(44)+PEEK(9):POKE46,PEEK(46)+PEEK(9):SYS42291:POKE58,99:RUN960 
960 GOSUB900:A=BR-64:E=A+63:N=.:GOSUB100:POKEA+42,240:POKEBR+1016,A/64 
962 N=ZS:A=53248:E=A+4095:GOSUB102,ZEICHEN:GOSUB90,SLS:RUN 