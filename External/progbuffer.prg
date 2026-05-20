#/ Controller version = 3.13.01
#/ Date = 5/20/2026 4:50 PM
#/ User remarks = 
#0
!PNAME=
!PDESC=

!---- Variables for using Debug ------------------
INT Disp_Enable = 0
!-------------------------------------------------

!----Variables for using Home---------------------
REAL OffsetVel, HomingVel, MaxDistance, HomingOffset, HomingCurrLimit, HardStopThreshold, HomingTimeOut
REAL TimeOut, Commut_current, HomingAcc, Cummut_Time
!-------------------------------------------------

OffsetVel = 5
INT iAXIS = 0
INT DEFAULT_VAL = 100
					
HomeMethod(iAXIS) = 37					
HomingVel = 100										! Not specified Default value: SLCPRD(AXIS)*EFAC/2
														
!MaxDistance = 100000								! Use More than Axis Full stroke					
HomingOffset = 0
HomingCurrLimit = XRMS(iAXIS)*0.7										
HardStopThreshold = CERRV(iAXIS)*0.75				! Not Specified Default Value : ABS(PE(AXIS))>CERRV(AXIS)*0.75 
TimeOut = 1000										! 1 Seconds [Unit : ms]
Cummut_Time = 500									! 0.5 Seconds [Unit : ms]
HomingTimeOut = 60 * TimeOut						! 60 Seconds [Unit : ms]

!----------------------------------------------------------		
if(PC_ACS_VELOCITY(iAXIS) = 0)
VEL(iAXIS) 	= DEFAULT_VAL
ACC(iAXIS) 	= DEFAULT_VAL *10
DEC(iAXIS) 	= DEFAULT_VAL *10
JERK(iAXIS)	= DEFAULT_VAL *100
KDEC(iAXIS)	= DEFAULT_VAL *100	
else
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
end
!---------------------------------------------------------			

DISABLE(iAXIS)
WAIT 100


ENABLE (iAXIS)
WAIT 1000

! Start Homing

HOME (iAXIS), HomeMethod(iAXIS), HomingVel, , HomingOffset, HomingCurrLimit, HardStopThreshold
TILL MFLAGS(iAXIS).#HOME, HomingTimeOut 
	IF ^MFLAGS(iAXIS).#HOME
		DISP"Axis %d Homing Failed", (iAXIS)
		GOTO Time_Out
	END
HomeFlag(iAXIS) = 1
STOP

Time_Out:
DISABLE (iAXIS)
HomeFlag(iAXIS) = 0
DISP"Home fault time_out, Axis number = %d, HomeFlag(%d) = %d", (iAXIS), (iAXIS), HomeFlag(iAXIS)
GOTO Restore
STOP

Restore:
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
STOP

! Servo-On
ON RD_Ena_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 0
RD_Ena_CMD(iAXIS) = 0

	if(Disp_Enable = 1) Disp "Servo On Command On. Axis No: %d",iAXIS ; end
	ENABLE (iAXIS)
	
	if(Disp_Enable = 1) Disp "Enable Completed. Axis No: %d" ,iAXIS ; end
	
	
RET
! --------------------------------------------------------------------------------------------------

! Servo-Off
ON RD_Disable_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 1
	RD_Disable_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) Disp "Servo Off Command On. Axis No: %d",iAXIS ; end
	DISABLE (iAXIS)
	if(Disp_Enable = 1) DISP "Disable Completed. Axis No: %d ", iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------
! Halt (Stop move)
ON RD_Halt_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0
	RD_Halt_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) DISP "Stop Move Command On. Axis No: %d" ,iAXIS ; end
	HALT (iAXIS)
	if(Disp_Enable = 1) DISP "Stop Move Completed. Axis No: %d" ,iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------

! Homing 
ON RD_Home_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0 
	RD_Home_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Homing Command On. Axis No: %d" ,iAXIS, iAXIS , RD_Home_CMD(iAXIS) ; end
	START (iAXIS),1
	
	if(Disp_Enable = 1) DISP "Homing Completed. Axis No: %d", iAXIS; end
RET
! --------------------------------------------------------------------------------------------------

! Fault clear
ON RD_Fcle_CMD(iAXIS) = 1
	RD_Fcle_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Fault Clear Command On. Axis No: %d" ,iAXIS ; end
	FCLEAR (iAXIS)
	if(Disp_Enable = 1) DISP "Fault Clear Completed. Axis No: %d" ,iAXIS ; end
	
RET
! --------------------------------------------------------------------------------------------------


! Interlock Check
Interlock_Check:
int interlockCheck
	if(MST(iAXIS).#MOVE <> 0) DISP "Motor is moving state. Interlock check failed. Axis No: %d", iAXIS; STOP; end;
!	if(PST(1).#RUN <> 0) DISP "Buffer is running state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
!	if(MERR(AXIS) <> 0) DISP "Buffer is error state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	
!	if(HomeFlag(0) <> 1) DISP "Buffer is not home. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	if(Disp_Enable = 1) DISP "Interlock Check Completed" ; end
RET
! --------------------------------------------------------------------------------------------------

! PTP Abs move
ON RD_Abs_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Abs_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),ABS_MOVE
RET
! --------------------------------------------------------------------------------------------------

! +ABS motion
ABS_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/E (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! --------------------------------------------------------------------------------------------------

! +Jog motion

ON RD_pJog_CMD(iAXIS) = 1
	START (iAXIS),pJOG_MOTION
RET

pJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end

	JOG (iAXIS),+
	DISP "Positive Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)
	
	
	TILL RD_pJog_CMD(iAXIS) = 0
	HALT(iAXIS)
STOP

! --------------------------------------------------------------------------------------------------

! -Jog motion
ON RD_nJog_CMD(iAXIS) = 1
	START (iAXIS),nJOG_MOTION
RET

nJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	
	JOG (iAXIS),-
	DISP "Negative Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)

	
	TILL RD_nJog_CMD(iAXIS) = 0
	HALT(iAXIS)
STOP

! --------------------------------------------------------------------------------------------------
! PTP Inc move
ON RD_Inc_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Inc_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),INC_MOVE
RET
! --------------------------------------------------------------------------------------------------

! Inc motion
INC_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/r (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! --------------------------------------------------------------------------------------------------
#1
!PNAME=
!PDESC=

!---- Variables for using Debug ------------------
INT Disp_Enable = 0
!-------------------------------------------------

!----Variables for using Home---------------------
REAL OffsetVel, HomingVel, MaxDistance, HomingOffset, HomingCurrLimit, HardStopThreshold, HomingTimeOut
REAL TimeOut, Commut_current, HomingAcc, Cummut_Time
!-------------------------------------------------

OffsetVel = 5
INT iAXIS = 1
INT DEFAULT_VAL = 100
					
HomeMethod(iAXIS) = 37					
HomingVel = 100										! Not specified Default value: SLCPRD(AXIS)*EFAC/2
														
!MaxDistance = 100000								! Use More than Axis Full stroke					
HomingOffset = 0
HomingCurrLimit = XRMS(iAXIS)*0.7										
HardStopThreshold = CERRV(iAXIS)*0.75				! Not Specified Default Value : ABS(PE(AXIS))>CERRV(AXIS)*0.75 
TimeOut = 1000										! 1 Seconds [Unit : ms]
Cummut_Time = 500									! 0.5 Seconds [Unit : ms]
HomingTimeOut = 60 * TimeOut						! 60 Seconds [Unit : ms]

!----------------------------------------------------------		
if(PC_ACS_VELOCITY(iAXIS) = 0)
VEL(iAXIS) 	= DEFAULT_VAL
ACC(iAXIS) 	= DEFAULT_VAL *10
DEC(iAXIS) 	= DEFAULT_VAL *10
JERK(iAXIS)	= DEFAULT_VAL *100
KDEC(iAXIS)	= DEFAULT_VAL *100	
else
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
end
!---------------------------------------------------------			

DISABLE(iAXIS)
WAIT 100
	

ENABLE (iAXIS)
WAIT 1000

! Start Homing

HOME (iAXIS), HomeMethod(iAXIS), HomingVel, , HomingOffset, HomingCurrLimit, HardStopThreshold
TILL MFLAGS(iAXIS).#HOME, HomingTimeOut 
	IF ^MFLAGS(iAXIS).#HOME
		DISP"Axis %d Homing Failed", (iAXIS)
		GOTO Time_Out
	END
HomeFlag(iAXIS) = 1
STOP

Time_Out:
DISABLE (iAXIS)
HomeFlag(iAXIS) = 0
DISP"Home fault time_out, Axis number = %d, HomeFlag(%d) = %d", (iAXIS), (iAXIS), HomeFlag(iAXIS)
GOTO Restore
STOP

Restore:
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
STOP

! Servo-On
ON RD_Ena_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 0
RD_Ena_CMD(iAXIS) = 0

	if(Disp_Enable = 1) Disp "Servo On Command On. Axis No: %d",iAXIS ; end
	ENABLE (iAXIS)
	
	if(Disp_Enable = 1) Disp "Enable Completed. Axis No: %d" ,iAXIS ; end
	
	
RET
! --------------------------------------------------------------------------------------------------

! Servo-Off
ON RD_Disable_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 1
	RD_Disable_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) Disp "Servo Off Command On. Axis No: %d",iAXIS ; end
	DISABLE (iAXIS)
	if(Disp_Enable = 1) DISP "Disable Completed. Axis No: %d ", iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------
! Halt (Stop move)
ON RD_Halt_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0
	RD_Halt_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) DISP "Stop Move Command On. Axis No: %d" ,iAXIS ; end
	HALT (iAXIS)
	if(Disp_Enable = 1) DISP "Stop Move Completed. Axis No: %d" ,iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------

! Homing 
ON RD_Home_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0 
	RD_Home_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Homing Command On. Axis No: %d" ,iAXIS, iAXIS , RD_Home_CMD(iAXIS) ; end
	START (iAXIS),1
	
	if(Disp_Enable = 1) DISP "Homing Completed. Axis No: %d", iAXIS; end
RET
! --------------------------------------------------------------------------------------------------

! Fault clear
ON RD_Fcle_CMD(iAXIS) = 1
	RD_Fcle_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Fault Clear Command On. Axis No: %d" ,iAXIS ; end
	FCLEAR (iAXIS)
	if(Disp_Enable = 1) DISP "Fault Clear Completed. Axis No: %d" ,iAXIS ; end
	
RET
! --------------------------------------------------------------------------------------------------


! Interlock Check
Interlock_Check:
int interlockCheck
	if(MST(iAXIS).#MOVE <> 0) DISP "Motor is moving state. Interlock check failed. Axis No: %d", iAXIS; STOP; end;
!	if(PST(1).#RUN <> 0) DISP "Buffer is running state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
!	if(MERR(AXIS) <> 0) DISP "Buffer is error state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	
!	if(HomeFlag(0) <> 1) DISP "Buffer is not home. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	if(Disp_Enable = 1) DISP "Interlock Check Completed" ; end
RET
! --------------------------------------------------------------------------------------------------

! PTP Abs move
ON RD_Abs_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Abs_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),ABS_MOVE
RET
! --------------------------------------------------------------------------------------------------

! +ABS motion
ABS_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/E (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! --------------------------------------------------------------------------------------------------

! +Jog motion

ON RD_pJog_CMD(iAXIS) = 1
	START (iAXIS),pJOG_MOTION
RET

pJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end

	JOG (iAXIS),+
	DISP "Positive Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)
	
	
	TILL RD_pJog_CMD(iAXIS) = 0
	HALT(iAXIS)
STOP

! --------------------------------------------------------------------------------------------------

! -Jog motion
ON RD_nJog_CMD(iAXIS) = 1
	START (iAXIS),nJOG_MOTION
RET

nJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	
	JOG (iAXIS),-
	DISP "Negative Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)

	
	TILL RD_nJog_CMD(iAXIS) = 0
	HALT(iAXIS)
STOP

! --------------------------------------------------------------------------------------------------

! PTP Inc move
ON RD_Inc_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Inc_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),INC_MOVE
RET
! --------------------------------------------------------------------------------------------------

! +Inc motion
INC_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/r (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! --------------------------------------------------------------------------------------------------
#2
!PNAME=
!PDESC=

!---- Variables for using Debug ------------------
INT Disp_Enable = 0
!-------------------------------------------------

!----Variables for using Home---------------------
REAL OffsetVel, HomingVel, MaxDistance, HomingOffset, HomingCurrLimit, HardStopThreshold, HomingTimeOut
REAL TimeOut, Commut_current, HomingAcc, Cummut_Time
!-------------------------------------------------

OffsetVel = 5
INT iAXIS = 2
INT DEFAULT_VAL = 100
					
HomeMethod(iAXIS) = 37					
HomingVel = 100										! Not specified Default value: SLCPRD(AXIS)*EFAC/2
														
!MaxDistance = 100000								! Use More than Axis Full stroke					
HomingOffset = 0
HomingCurrLimit = XRMS(iAXIS)*0.7										
HardStopThreshold = CERRV(iAXIS)*0.75				! Not Specified Default Value : ABS(PE(AXIS))>CERRV(AXIS)*0.75 
TimeOut = 1000										! 1 Seconds [Unit : ms]
Cummut_Time = 500									! 0.5 Seconds [Unit : ms]
HomingTimeOut = 60 * TimeOut						! 60 Seconds [Unit : ms]

!----------------------------------------------------------		
if(PC_ACS_VELOCITY(iAXIS) = 0)
VEL(iAXIS) 	= DEFAULT_VAL
ACC(iAXIS) 	= DEFAULT_VAL *10
DEC(iAXIS) 	= DEFAULT_VAL *10
JERK(iAXIS)	= DEFAULT_VAL *100
KDEC(iAXIS)	= DEFAULT_VAL *100	
else
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
end
!---------------------------------------------------------			

DISABLE(iAXIS)
WAIT 100
	

ENABLE (iAXIS)
WAIT 1000

! Start Homing

HOME (iAXIS), HomeMethod(iAXIS), HomingVel, , HomingOffset, HomingCurrLimit, HardStopThreshold
TILL MFLAGS(iAXIS).#HOME, HomingTimeOut 
	IF ^MFLAGS(iAXIS).#HOME
		DISP"Axis %d Homing Failed", (iAXIS)
		GOTO Time_Out
	END
HomeFlag(iAXIS) = 1
STOP

Time_Out:
DISABLE (iAXIS)
HomeFlag(iAXIS) = 0
DISP"Home fault time_out, Axis number = %d, HomeFlag(%d) = %d", (iAXIS), (iAXIS), HomeFlag(iAXIS)
GOTO Restore
STOP

Restore:
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
STOP

! Servo-On
ON RD_Ena_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 0
RD_Ena_CMD(iAXIS) = 0

	if(Disp_Enable = 1) Disp "Servo On Command On. Axis No: %d",iAXIS ; end
	ENABLE (iAXIS)
	
	if(Disp_Enable = 1) Disp "Enable Completed. Axis No: %d" ,iAXIS ; end
	
	
RET
! --------------------------------------------------------------------------------------------------

! Servo-Off
ON RD_Disable_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 1
	RD_Disable_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) Disp "Servo Off Command On. Axis No: %d",iAXIS ; end
	DISABLE (iAXIS)
	if(Disp_Enable = 1) DISP "Disable Completed. Axis No: %d ", iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------
! Halt (Stop move)
ON RD_Halt_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0
	RD_Halt_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) DISP "Stop Move Command On. Axis No: %d" ,iAXIS ; end
	HALT (iAXIS)
	if(Disp_Enable = 1) DISP "Stop Move Completed. Axis No: %d" ,iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------

! Homing 
ON RD_Home_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0 
	RD_Home_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Homing Command On. Axis No: %d" ,iAXIS, iAXIS , RD_Home_CMD(iAXIS) ; end
	START (iAXIS),1
	
	if(Disp_Enable = 1) DISP "Homing Completed. Axis No: %d", iAXIS; end
RET
! --------------------------------------------------------------------------------------------------

! Fault clear
ON RD_Fcle_CMD(iAXIS) = 1
	RD_Fcle_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Fault Clear Command On. Axis No: %d" ,iAXIS ; end
	FCLEAR (iAXIS)
	if(Disp_Enable = 1) DISP "Fault Clear Completed. Axis No: %d" ,iAXIS ; end
	
RET
! --------------------------------------------------------------------------------------------------


! Interlock Check
Interlock_Check:
int interlockCheck
	if(MST(iAXIS).#MOVE <> 0) DISP "Motor is moving state. Interlock check failed. Axis No: %d", iAXIS; STOP; end;
!	if(PST(1).#RUN <> 0) DISP "Buffer is running state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
!	if(MERR(AXIS) <> 0) DISP "Buffer is error state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	
!	if(HomeFlag(0) <> 1) DISP "Buffer is not home. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	if(Disp_Enable = 1) DISP "Interlock Check Completed" ; end
RET
! --------------------------------------------------------------------------------------------------

! PTP Abs move
ON RD_Abs_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Abs_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),ABS_MOVE
RET
! --------------------------------------------------------------------------------------------------

! +ABS motion
ABS_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/E (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! --------------------------------------------------------------------------------------------------

! +Jog motion

ON RD_pJog_CMD(iAXIS) = 1
	START (iAXIS),pJOG_MOTION
RET

pJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end

	JOG (iAXIS),+
	DISP "Positive Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)
	
	
	TILL RD_pJog_CMD(iAXIS) = 0
	HALT(iAXIS)
STOP

! --------------------------------------------------------------------------------------------------

! -Jog motion
ON RD_nJog_CMD(iAXIS) = 1
	START (iAXIS),nJOG_MOTION
RET

nJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	
	JOG (iAXIS),-
	DISP "Negative Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)

	
	TILL RD_nJog_CMD(iAXIS) = 0
	HALT(iAXIS)
STOP

! --------------------------------------------------------------------------------------------------
! PTP Inc move
ON RD_Inc_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Inc_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),INC_MOVE
RET
! --------------------------------------------------------------------------------------------------

! +Inc motion
INC_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/r (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! --------------------------------------------------------------------------------------------------
#3
!PNAME=
!PDESC=

!---- Variables for using Debug ------------------
INT Disp_Enable = 0
!-------------------------------------------------

!----Variables for using Home---------------------
REAL OffsetVel, HomingVel, MaxDistance, HomingOffset, HomingCurrLimit, HardStopThreshold, HomingTimeOut
REAL TimeOut, Commut_current, HomingAcc, Cummut_Time
!-------------------------------------------------

OffsetVel = 5
INT iAXIS = 3
INT DEFAULT_VAL = 100
					
HomeMethod(iAXIS) = 37			
HomingVel = 100										! Not specified Default value: SLCPRD(AXIS)*EFAC/2
														
!MaxDistance = 100000								! Use More than Axis Full stroke					
HomingOffset = 0
HomingCurrLimit = XRMS(iAXIS)*0.7										
HardStopThreshold = CERRV(iAXIS)*0.75				! Not Specified Default Value : ABS(PE(AXIS))>CERRV(AXIS)*0.75 
TimeOut = 1000										! 1 Seconds [Unit : ms]
Cummut_Time = 500									! 0.5 Seconds [Unit : ms]
HomingTimeOut = 60 * TimeOut						! 60 Seconds [Unit : ms]

!----------------------------------------------------------		
if(PC_ACS_VELOCITY(iAXIS) = 0)
VEL(iAXIS) 	= DEFAULT_VAL
ACC(iAXIS) 	= DEFAULT_VAL *10
DEC(iAXIS) 	= DEFAULT_VAL *10
JERK(iAXIS)	= DEFAULT_VAL *100
KDEC(iAXIS)	= DEFAULT_VAL *100	
else
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
end
!---------------------------------------------------------			

DISABLE(iAXIS)
WAIT 100
		

ENABLE (iAXIS)
WAIT 1000

! Start Homing

HOME (iAXIS), HomeMethod(iAXIS), HomingVel, , HomingOffset, HomingCurrLimit, HardStopThreshold
TILL MFLAGS(iAXIS).#HOME, HomingTimeOut 
	IF ^MFLAGS(iAXIS).#HOME
		DISP"Axis %d Homing Failed", (iAXIS)
		GOTO Time_Out
	END
HomeFlag(iAXIS) = 1
STOP

Time_Out:
DISABLE (iAXIS)
HomeFlag(iAXIS) = 0
DISP"Home fault time_out, Axis number = %d, HomeFlag(%d) = %d", (iAXIS), (iAXIS), HomeFlag(iAXIS)
GOTO Restore
STOP

Restore:
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
STOP

! Servo-On
ON RD_Ena_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 0
RD_Ena_CMD(iAXIS) = 0
	if(Disp_Enable = 1) Disp "Servo On Command On. Axis No: %d",iAXIS ; end
	ENABLE (iAXIS)
	
	if(Disp_Enable = 1) Disp "Enable Completed. Axis No: %d" ,iAXIS ; end
	
	
RET
! --------------------------------------------------------------------------------------------------

! Servo-Off
ON RD_Disable_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 1
	RD_Disable_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) Disp "Servo Off Command On. Axis No: %d",iAXIS ; end
	DISABLE (iAXIS)
	if(Disp_Enable = 1) DISP "Disable Completed. Axis No: %d ", iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------
! Halt (Stop move)
ON RD_Halt_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0
	RD_Halt_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) DISP "Stop Move Command On. Axis No: %d" ,iAXIS ; end
	HALT (iAXIS)
	if(Disp_Enable = 1) DISP "Stop Move Completed. Axis No: %d" ,iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------

! Homing 
ON RD_Home_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0 
	RD_Home_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Homing Command On. Axis No: %d" ,iAXIS, iAXIS , RD_Home_CMD(iAXIS) ; end
	START (iAXIS),1
	
	if(Disp_Enable = 1) DISP "Homing Completed. Axis No: %d", iAXIS; end
RET
! --------------------------------------------------------------------------------------------------

! Fault clear
ON RD_Fcle_CMD(iAXIS) = 1
	RD_Fcle_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Fault Clear Command On. Axis No: %d" ,iAXIS ; end
	FCLEAR (iAXIS)
	if(Disp_Enable = 1) DISP "Fault Clear Completed. Axis No: %d" ,iAXIS ; end
	
RET
! --------------------------------------------------------------------------------------------------


! Interlock Check
Interlock_Check:
int interlockCheck
	if(MST(iAXIS).#MOVE <> 0) DISP "Motor is moving state. Interlock check failed. Axis No: %d", iAXIS; STOP; end;
!	if(PST(1).#RUN <> 0) DISP "Buffer is running state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
!	if(MERR(AXIS) <> 0) DISP "Buffer is error state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	
!	if(HomeFlag(0) <> 1) DISP "Buffer is not home. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	if(Disp_Enable = 1) DISP "Interlock Check Completed" ; end
RET
! --------------------------------------------------------------------------------------------------

! PTP Abs move
ON RD_Abs_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Abs_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),ABS_MOVE
RET
! --------------------------------------------------------------------------------------------------

! +ABS motion
ABS_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/E (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! --------------------------------------------------------------------------------------------------

! +Jog motion

ON RD_pJog_CMD(iAXIS) = 1
	START (iAXIS),pJOG_MOTION
RET

pJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end

	JOG (iAXIS),+
	DISP "Positive Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)
	
	
	TILL RD_pJog_CMD(iAXIS) = 0
	HALT(iAXIS)
STOP

! --------------------------------------------------------------------------------------------------

! -Jog motion
ON RD_nJog_CMD(iAXIS) = 1
	START (iAXIS),nJOG_MOTION
RET

nJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	
	JOG (iAXIS),-
	DISP "Negative Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)

	
	TILL RD_nJog_CMD(iAXIS) = 0
	HALT(iAXIS)
STOP

! --------------------------------------------------------------------------------------------------
! PTP Inc move
ON RD_Inc_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Inc_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),INC_MOVE
RET
! --------------------------------------------------------------------------------------------------

! +Inc motion
INC_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/r (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! --------------------------------------------------------------------------------------------------
#4
!PNAME=
!PDESC=

!---- Variables for using Debug ------------------
INT Disp_Enable = 0
!-------------------------------------------------

!----Variables for using Home---------------------
REAL OffsetVel, HomingVel, MaxDistance, HomingOffset, HomingCurrLimit, HardStopThreshold, HomingTimeOut
REAL TimeOut, Commut_current, HomingAcc, Cummut_Time
!-------------------------------------------------

OffsetVel = 5
INT iAXIS = 4
INT DEFAULT_VAL = 100
					
HomeMethod(iAXIS) = 37					
HomingVel = 100										! Not specified Default value: SLCPRD(AXIS)*EFAC/2
														
!MaxDistance = 100000								! Use More than Axis Full stroke					
HomingOffset = 0
HomingCurrLimit = XRMS(iAXIS)*0.7										
HardStopThreshold = CERRV(iAXIS)*0.75				! Not Specified Default Value : ABS(PE(AXIS))>CERRV(AXIS)*0.75 
TimeOut = 1000										! 1 Seconds [Unit : ms]
Cummut_Time = 500									! 0.5 Seconds [Unit : ms]
HomingTimeOut = 60 * TimeOut						! 60 Seconds [Unit : ms]

!----------------------------------------------------------		
if(PC_ACS_VELOCITY(iAXIS) = 0)
VEL(iAXIS) 	= DEFAULT_VAL
ACC(iAXIS) 	= DEFAULT_VAL *10
DEC(iAXIS) 	= DEFAULT_VAL *10
JERK(iAXIS)	= DEFAULT_VAL *100
KDEC(iAXIS)	= DEFAULT_VAL *100	
else
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
end
!---------------------------------------------------------			

DISABLE(iAXIS)
WAIT 100


ENABLE (iAXIS)
WAIT 1000

! Start Homing

HOME (iAXIS), HomeMethod(iAXIS), HomingVel, , HomingOffset, HomingCurrLimit, HardStopThreshold
TILL MFLAGS(iAXIS).#HOME, HomingTimeOut 
	IF ^MFLAGS(iAXIS).#HOME
		DISP"Axis %d Homing Failed", (iAXIS)
		GOTO Time_Out
	END
HomeFlag(iAXIS) = 1
STOP

Time_Out:
DISABLE (iAXIS)
HomeFlag(iAXIS) = 0
DISP"Home fault time_out, Axis number = %d, HomeFlag(%d) = %d", (iAXIS), (iAXIS), HomeFlag(iAXIS)
GOTO Restore
STOP

Restore:
VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
ACC(iAXIS) = PC_ACS_ACC(iAXIS)
DEC(iAXIS) = PC_ACS_DEC(iAXIS)
JERK(iAXIS) = PC_ACS_JERK(iAXIS)
KDEC(iAXIS) = PC_ACS_JERK(iAXIS)
STOP

! Servo-On
ON RD_Ena_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 0
RD_Ena_CMD(iAXIS) = 0

	if(Disp_Enable = 1) Disp "Servo On Command On. Axis No: %d",iAXIS ; end
	ENABLE (iAXIS)
	
	if(Disp_Enable = 1) Disp "Enable Completed. Axis No: %d" ,iAXIS ; end
	
	
RET
! --------------------------------------------------------------------------------------------------

! Servo-Off
ON RD_Disable_CMD(iAXIS) = 1 & MST(iAXIS).#ENABLED = 1
	RD_Disable_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) Disp "Servo Off Command On. Axis No: %d",iAXIS ; end
	DISABLE (iAXIS)
	if(Disp_Enable = 1) DISP "Disable Completed. Axis No: %d ", iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------
! Halt (Stop move)
ON RD_Halt_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0
	RD_Halt_CMD(iAXIS) = 0
	
	if(Disp_Enable = 1) DISP "Stop Move Command On. Axis No: %d" ,iAXIS ; end
	HALT (iAXIS)
	if(Disp_Enable = 1) DISP "Stop Move Completed. Axis No: %d" ,iAXIS ; end
RET
! --------------------------------------------------------------------------------------------------

! Homing 
ON RD_Home_CMD(iAXIS) = 1 & PST(iAXIS).#RUN = 0 
	RD_Home_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Homing Command On. Axis No: %d" ,iAXIS, iAXIS , RD_Home_CMD(iAXIS) ; end
	START (iAXIS),1
	
	if(Disp_Enable = 1) DISP "Homing Completed. Axis No: %d", iAXIS; end
RET
! --------------------------------------------------------------------------------------------------

! Fault clear
ON RD_Fcle_CMD(iAXIS) = 1
	RD_Fcle_CMD(iAXIS) = 0
	if(Disp_Enable = 1) DISP "Fault Clear Command On. Axis No: %d" ,iAXIS ; end
	FCLEAR (iAXIS)
	if(Disp_Enable = 1) DISP "Fault Clear Completed. Axis No: %d" ,iAXIS ; end
	
RET
! --------------------------------------------------------------------------------------------------


! Interlock Check
Interlock_Check:
int interlockCheck
	if(MST(iAXIS).#MOVE <> 0) DISP "Motor is moving state. Interlock check failed. Axis No: %d", iAXIS; STOP; end;
!	if(PST(1).#RUN <> 0) DISP "Buffer is running state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
!	if(MERR(AXIS) <> 0) DISP "Buffer is error state. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	
!	if(HomeFlag(0) <> 1) DISP "Buffer is not home. Interlock check failed. Axis No: %d", AXIS; STOP; end;
	if(Disp_Enable = 1) DISP "Interlock Check Completed" ; end
RET
! --------------------------------------------------------------------------------------------------

! PTP Abs move
ON RD_Abs_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Abs_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),ABS_MOVE
RET
! --------------------------------------------------------------------------------------------------

! +ABS motion
ABS_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/E (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! +Jog motion

ON RD_pJog_CMD(iAXIS) = 1
	START (iAXIS),pJOG_MOTION
RET

pJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end

	JOG (iAXIS),+
	DISP "Positive Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)
	
	
	TILL RD_pJog_CMD(iAXIS) = 0
	HALT(iAXIS)
STOP

! --------------------------------------------------------------------------------------------------

! -Jog motion
ON RD_nJog_CMD(iAXIS) = 1
	START (iAXIS),nJOG_MOTION
RET

nJOG_MOTION:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	
	JOG (iAXIS),-
	DISP "Negative Jog Move %d AXIS, %fmm/s",iAXIS ,PC_ACS_VELOCITY(iAXIS)

	
	TILL RD_nJog_CMD(iAXIS) = 0
	HALT(iAXIS)
	
STOP

! --------------------------------------------------------------------------------------------------
! PTP Inc move
ON RD_Inc_CMD(iAXIS) = 1 & MST(iAXIS).#MOVE = 0 & PST(iAXIS).#RUN = 0
	RD_Inc_CMD(iAXIS) = 0
	CALL Interlock_Check
	START (iAXIS),INC_MOVE
RET
! --------------------------------------------------------------------------------------------------

! +Inc motion
INC_MOVE:
	CALL Interlock_Check
	if(PC_ACS_VELOCITY(iAXIS) <>0)
 	 VEL(iAXIS) = PC_ACS_VELOCITY(iAXIS)
   	end
	
	if(PC_ACS_ACC(iAXIS) <>0)
 	 ACC(iAXIS) = PC_ACS_ACC(iAXIS); DEC(iAXIS) = PC_ACS_DEC(iAXIS); JERK(iAXIS) = PC_ACS_JERK(iAXIS); KDEC(iAXIS) = PC_ACS_JERK(iAXIS); end;
   	
	PTP/r (iAXIS),PC_ACS_DISTANCE(iAXIS)
	
STOP
! --------------------------------------------------------------------------------------------------
#7
!PNAME=
!PDESC=
! (M-NT) In-position Criterion.prg
INT iXaxis, iYaxis
iXaxis = 0
iYaxis = 1

VEL(iXaxis) = 10000             ! Set maximum velocity
ACC(iXaxis) = 1000000           ! Set acceleration
DEC(iXaxis) = 1000000           ! Set deceleration
JERK(iXaxis) = 10000000         ! Set jerk
KDEC(iXaxis) = 10000000         ! Set kill deceleration

VEL(iYaxis) = 10000             ! Set maximum velocity
ACC(iYaxis) = 1000000           ! Set acceleration
DEC(iYaxis) = 1000000           ! Set deceleration
JERK(iYaxis) = 10000000         ! Set jerk
KDEC(iYaxis) = 10000000         ! Set kill deceleration

SETTLE(iXaxis) = 200            ! Set iXaxis settle time to 200msec
TARGRAD(iXaxis) = 0.3           ! Set iXaxis target envelop to +/-0.3 degrees
SETTLE(iYaxis) = 300            ! Set iYaxis settle time to 300msec
TARGRAD(iYaxis) = 0.8           ! Set iYaxis target envelop to +/-0.8 degrees

SET RPOS(iXaxis) = 0; SET RPOS(iYaxis) = 0

ENABLE (iXaxis, iYaxis)
LOOP 3                          ! Do the loop three times
  PTP (iXaxis),1000; PTP (iYaxis),500 ! Move to the first point
  TILL ^MST(iXaxis).#MOVE       ! Wait for end of longer motion
  WAIT 1000                     ! Wait 1000msec for the drill
  PTP (iXaxis),-1000; PTP (iYaxis),-500 ! Move to the second point
  TILL ^MST(iXaxis).#MOVE       ! Wait for end of the longer motion
  WAIT 1000                     ! Wait 1000msec for the drill
END
OUT(0).1 = 0                    ! Resets OUT#1 
PTP/e (iXaxis,iYaxis),0,0       ! Move to the initial point
DISABLE (iXaxis,iYaxis)
STOP

! The Autoroutine activates Out#1 when:
! 1. The axes are not in initial position
! 2. The axes are within the In-position requirments
ON (RPOS(iXaxis)>5 | RPOS(iXaxis)<-5) & (RPOS(iYaxis)>5 | RPOS(iYaxis)<-5) & ^MST(iXaxis).#MOVE & ^MST(iYaxis).#MOVE 
  OUT(0).1=1
RET

! The Autoroutine resets  out#1 when the axes are not in-position.
ON MST(iXaxis).#MOVE | MST(iYaxis).#MOVE
  OUT(0).1=0
RET

#9
!PNAME=
!PDESC=

while(ON_MONITORING_FLAG)
ACS_PC_CURRENT_POS_AXIS0 = FPOS(0)
ACS_PC_IS_HOME_AXIS0 = MFLAGS(0).#HOME
ACS_PC_IS_ENABLED_AXIS0 = MST(0).#ENABLED
ACS_PC_IS_INPOS_AXIS0 = MST(0).#INPOS
ACS_PC_IS_MOVE_AXIS0 = MST(0).#MOVE
ACS_PC_IS_FAULT_AXIS0 = FAULT(0).#SP
ACS_PC_IS_P_LIMIT_AXIS0 = FAULT(0).#SLL
ACS_PC_IS_N_LIMIT_AXIS0 = FAULT(0).#SRL

ACS_PC_CURRENT_POS_AXIS1 = FPOS(1)
ACS_PC_IS_HOME_AXIS1 = MFLAGS(1).#HOME
ACS_PC_IS_ENABLED_AXIS1 = MST(1).#ENABLED
ACS_PC_IS_INPOS_AXIS1 = MST(1).#INPOS
ACS_PC_IS_MOVE_AXIS1 = MST(1).#MOVE
ACS_PC_IS_FAULT_AXIS1 = FAULT(1).#SP
ACS_PC_IS_P_LIMIT_AXIS1 = FAULT(1).#SLL
ACS_PC_IS_N_LIMIT_AXIS1 = FAULT(1).#SRL

ACS_PC_CURRENT_POS_AXIS2 = FPOS(2)
ACS_PC_IS_HOME_AXIS2 = MFLAGS(2).#HOME
ACS_PC_IS_ENABLED_AXIS2 = MST(2).#ENABLED
ACS_PC_IS_INPOS_AXIS2 = MST(2).#INPOS
ACS_PC_IS_MOVE_AXIS2 = MST(2).#MOVE
ACS_PC_IS_FAULT_AXIS2 = FAULT(2).#SP
ACS_PC_IS_P_LIMIT_AXIS2 = FAULT(2).#SLL
ACS_PC_IS_N_LIMIT_AXIS2 = FAULT(2).#SRL

ACS_PC_CURRENT_POS_AXIS3 = FPOS(3)
ACS_PC_IS_HOME_AXIS3 = MFLAGS(3).#HOME
ACS_PC_IS_ENABLED_AXIS3 = MST(3).#ENABLED
ACS_PC_IS_INPOS_AXIS3 = MST(3).#INPOS
ACS_PC_IS_MOVE_AXIS3 = MST(3).#MOVE
ACS_PC_IS_FAULT_AXIS3 = FAULT(3).#SP
ACS_PC_IS_P_LIMIT_AXIS3 = FAULT(3).#SLL
ACS_PC_IS_N_LIMIT_AXIS3 = FAULT(3).#SRL

ACS_PC_CURRENT_POS_AXIS4 = FPOS(4)
ACS_PC_IS_HOME_AXIS4 = MFLAGS(4).#HOME
ACS_PC_IS_ENABLED_AXIS4 = MST(4).#ENABLED
ACS_PC_IS_INPOS_AXIS4 = MST(4).#INPOS
ACS_PC_IS_MOVE_AXIS4 = MST(4).#MOVE
ACS_PC_IS_FAULT_AXIS4 = FAULT(4).#SP
ACS_PC_IS_P_LIMIT_AXIS4 = FAULT(4).#SLL
ACS_PC_IS_N_LIMIT_AXIS4 = FAULT(4).#SRL

end;

Stop;

#A
!PNAME=
!PDESC=
global real PC_ACS_DISTANCE(64)
global real PC_ACS_VELOCITY(64)
global real PC_ACS_ACC(64)
global real PC_ACS_DEC(64)
global real PC_ACS_JERK(64)

global int CMD_ABS_MOVE(64)
global int CMD_RESET(64)
global int CMD_STOP(64)

global int ON_MONITORING_FLAG

global real ACS_PC_CURRENT_POS_AXIS0
global int ACS_PC_IS_HOME_AXIS0
global int ACS_PC_IS_ENABLED_AXIS0
global int ACS_PC_IS_INPOS_AXIS0
global int ACS_PC_IS_MOVE_AXIS0
global int ACS_PC_IS_FAULT_AXIS0
global int ACS_PC_IS_P_LIMIT_AXIS0
global int ACS_PC_IS_N_LIMIT_AXIS0

global real ACS_PC_CURRENT_POS_AXIS1
global int ACS_PC_IS_HOME_AXIS1
global int ACS_PC_IS_ENABLED_AXIS1
global int ACS_PC_IS_INPOS_AXIS1
global int ACS_PC_IS_MOVE_AXIS1
global int ACS_PC_IS_FAULT_AXIS1
global int ACS_PC_IS_P_LIMIT_AXIS1
global int ACS_PC_IS_N_LIMIT_AXIS1

global real ACS_PC_CURRENT_POS_AXIS2
global int ACS_PC_IS_HOME_AXIS2
global int ACS_PC_IS_ENABLED_AXIS2
global int ACS_PC_IS_INPOS_AXIS2
global int ACS_PC_IS_MOVE_AXIS2
global int ACS_PC_IS_FAULT_AXIS2
global int ACS_PC_IS_P_LIMIT_AXIS2
global int ACS_PC_IS_N_LIMIT_AXIS2

global real ACS_PC_CURRENT_POS_AXIS3
global int ACS_PC_IS_HOME_AXIS3
global int ACS_PC_IS_ENABLED_AXIS3
global int ACS_PC_IS_INPOS_AXIS3
global int ACS_PC_IS_MOVE_AXIS3
global int ACS_PC_IS_FAULT_AXIS3
global int ACS_PC_IS_P_LIMIT_AXIS3
global int ACS_PC_IS_N_LIMIT_AXIS3

global real ACS_PC_CURRENT_POS_AXIS4
global int ACS_PC_IS_HOME_AXIS4
global int ACS_PC_IS_ENABLED_AXIS4
global int ACS_PC_IS_INPOS_AXIS4
global int ACS_PC_IS_MOVE_AXIS4
global int ACS_PC_IS_FAULT_AXIS4
global int ACS_PC_IS_P_LIMIT_AXIS4
global int ACS_PC_IS_N_LIMIT_AXIS4

GLOBAL int HomeMethod(64)
GLOBAL int HomeFlag(64)

global int RD_Ena_CMD(64)
global int RD_Disable_CMD(64)
global int RD_Halt_CMD(64)
global int RD_Fcle_CMD(64)
global int RD_Abs_CMD(64)
global int RD_Inc_CMD(64)
global int RD_pJog_CMD(64)
global int RD_nJog_CMD(64)
global int RD_Home_CMD(64)


