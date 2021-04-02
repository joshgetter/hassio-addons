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

    def __init__(self):
        self._isCameraEnabled = False
        self._observers = []
        global Event
        Event = namedtuple('Event', 'changedProperty state')

