from collections import namedtuple
class State:

    def bind_to(self, callback):
        self._observers.append(callback)

    def notifyObservers(self, propertyName):
        for callback in self._observers:
            callback(Event(changedProperty=propertyName, state=self))

    @property
    def isCameraEnabled(self):
        return self._isCameraEnabled

    @isCameraEnabled.setter
    def isCameraEnabled(self, value):
        oldValue = self._isCameraEnabled
        self._isCameraEnabled = value
        if(oldValue != value):
            self.notifyObservers('isCameraEnabled')

    @property
    def isErrored(self):
        return self._isErrored

    @isErrored.setter
    def isErrored(self, value):
        oldValue = self._isErrored
        self._isErrored = value
        if(oldValue != value):
            self.notifyObservers('isErrored')

    @property
    def errorCount(self):
        return self._errorCount

    @errorCount.setter
    def errorCount(self, value):
        oldValue = self._errorCount
        self._errorCount = value
        if(oldValue != value):
            self.notifyObservers('errorCount')
            
    @property
    def isRunning(self):
        return self._isRunning
    
    @isRunning.setter
    def isRunning(self, value):
        oldValue = self._isRunning
        self._isRunning = value
        if(oldValue != value):
            self.notifyObservers('isRunning')

    def __init__(self):
        self._isCameraEnabled = False
        self._isErrored = False
        self._isRunning = False
        self._errorCount = 0
        self._observers = []
        global Event
        Event = namedtuple('Event', 'changedProperty state')

